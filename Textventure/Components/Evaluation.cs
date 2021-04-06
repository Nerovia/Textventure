using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Textventure.Resources;

namespace Textventure.Components
{
    public class Requirement
    {
        #region Public

        public string Expression { get; }

        public Requirement(string expression)
        {
            Expression = expression;
            binaryTreeRoot = ParseBinaryTree(ParsePostfix(expression));
        }

        public static Requirement Parse(string expression)
        {
            if (expression is null)
                return null;
            try
            {
                return new Requirement(expression);
            }
            catch
            {
                return null;
            }
        }

        public static bool Evaluate(string expression, ReferenceCallback callback)
        {
            return new Requirement(expression).Evaluate(callback);
        }

        public bool Evaluate(ReferenceCallback callback)
        {
            return binaryTreeRoot.Evaluate(callback);
        }


        #endregion

        #region Private


        private Node binaryTreeRoot;

        private Queue<string> ParsePostfix(string expression)
        {
            var outputQueue = new Queue<string>();
            var operatorStack = new Stack<string>();
            var operators = new char[] { '*', '+', '!', '(', ')' };
            string buffer = "";

            var PushOperator = new Action<string>((string o) =>
            {
                switch (o)
                {
                    case "!":
                        operatorStack.Push(o);
                        break;

                    case "*":
                        while (operatorStack.Count > 0)
                        {
                            var p = operatorStack.Peek();
                            if (p == "*" || p == "!")
                                outputQueue.Enqueue(operatorStack.Pop());
                            else
                                break;
                        }
                        operatorStack.Push(o);
                        break;

                    case "+":
                        while (operatorStack.Count > 0)
                        {
                            var p = operatorStack.Peek();
                            if (p == "*" || p == "!" || p == "+")
                                outputQueue.Enqueue(operatorStack.Pop());
                            else
                                break;
                        }
                        operatorStack.Push(o);
                        break;

                    case "(":
                        operatorStack.Push(o);
                        break;

                    case ")":
                        while (operatorStack.Count > 0)
                        {
                            var e = operatorStack.Pop();
                            if (e == "(")
                                return;
                            else
                                outputQueue.Enqueue(e);
                        }
                        throw new InvalidExpressionException()
                        {
                            Expression = Expression,
                            Error = "missing opening bracket",
                        };
                        break;
                }
            });

            foreach (var c in expression)
            {
                if (char.IsWhiteSpace(c))
                {
                    continue;
                }


                if (operators.Contains(c))
                {
                    if (buffer.Length > 0)
                    {
                        outputQueue.Enqueue(buffer);
                        buffer = "";
                    }

                    PushOperator(c.ToString());
                }
                else
                {
                    buffer += c;
                }
            }

            if (buffer.Length > 0)
                outputQueue.Enqueue(buffer);

            while (operatorStack.Count > 0)
            {
                var o = operatorStack.Pop();
                if (o == "(")
                    throw new InvalidExpressionException()
                    {
                        Expression = Expression,
                        Error = "missing closing bracket"
                    };
                else
                    outputQueue.Enqueue(o);
            }

            return outputQueue;
        }

        private Node ParseBinaryTree(Queue<string> postfixExpression)
        {
            Stack<Node> nodeStack = new Stack<Node>();

            while (postfixExpression.Count > 0)
            {
                var e = postfixExpression.Dequeue();
                switch (e)
                {
                    case "+":
                        nodeStack.Push(new OrNode(nodeStack.Pop(), nodeStack.Pop()));
                        break;

                    case "*":
                        nodeStack.Push(new AndNode(nodeStack.Pop(), nodeStack.Pop()));
                        break;

                    case "!":
                        nodeStack.Push(new NotNode(nodeStack.Pop()));
                        break;

                    default:
                        nodeStack.Push(new VariableNode(new Reference(e)));
                        break;
                }
            }

            if (nodeStack.Count != 1)
                throw new InvalidExpressionException()
                {
                    Expression = Expression,
                };

            return nodeStack.Pop();
        }


        #endregion

        #region Nodes

        private abstract class Node
        {
            public abstract bool Evaluate(ReferenceCallback callback);
        }

        private class AndNode : Node
        {
            Node a;
            Node b;

            public AndNode(Node a, Node b)
            {
                this.a = a;
                this.b = b;
            }

            public override bool Evaluate(ReferenceCallback callback)
            {
                return a.Evaluate(callback) && b.Evaluate(callback);
            }
        }

        private class OrNode : Node
        {
            Node a;
            Node b;

            public OrNode(Node a, Node b)
            {
                this.a = a;
                this.b = b;
            }

            public override bool Evaluate(ReferenceCallback callback)
            {
                return a.Evaluate(callback) || b.Evaluate(callback);
            }
        }

        private class NotNode : Node
        {
            Node a;

            public NotNode(Node a)
            {
                this.a = a;
            }

            public override bool Evaluate(ReferenceCallback callback)
            {
                return !a.Evaluate(callback);
            }
        }

        private class VariableNode : Node
        {
            Reference reference;

            public VariableNode(Reference reference)
            {
                this.reference = reference;
            }

            public override bool Evaluate(ReferenceCallback callback)
            {
                return callback(reference);
            }
        }

        #endregion
    }

    public delegate bool ReferenceCallback(Reference variable);

    public class InvalidExpressionException : Exception
    {
        public override string Message { get => $"Could not parse expression: \"{{{Expression}}}\" because of \"{Error}\""; }
        public string Expression { get; set; }
        public string Error { get; set; }
    }

    public class EvaluationResult
    {
        public string description = string.Empty;

        public List<Impact> impacts = new List<Impact>();
    }

    public static class Tag
    {
        public static SortedSet<string> ParseCollection(string expression)
        {
            if (expression is null)
                return new SortedSet<string>();
            var regex = new Regex(@"[^\s,]\s+\S");
            if (regex.IsMatch(expression))
                throw new Exception("Invalid Syntax");
            return new SortedSet<string>(expression.SplitTrimmed(','));
        }
    }

    public class Reference
    {
        public Reference(string expression)
        {
            if (expression is null)
                throw new ArgumentNullException();

            var split = expression.Split('.');

            if (split.Length > 2)
                throw new ArgumentException();

            if (split.Length == 1)
            {
                IsLocal = true;
                Property = split[0];
            }
            else
            {
                Element = split[0];
                Property = split[1];
                if (Element.ToLower() == "inventory")
                    IsInventory = true;
                else if (Element.ToLower() == "player")
                    IsPlayer = true;
            }
        }

        public bool IsInventory { get; } = false;
        public bool IsPlayer { get; } = false;
        public bool IsLocal { get; } = false;
        public string Element { get; } = null;
        public string Property { get; } = null;

        public override string ToString() => $"{Element}.{Property}";
    }

    public enum Manipulation
    {
        Add,
        Remove,
        Invert,
    }

    public class Impact
    {
        public static Impact[] ParseCollection(string expression)
        {
            if (expression is null)
                return null;
            var expressions = expression.Split(',');
            var impacts = new List<Impact>();
            foreach (var e in expressions)
                impacts.Add(Parse(e));
            return impacts.ToArray();
        }

        public static Impact Parse(string expression)
        {
            expression = expression.Trim();
            if (expression.Length < 2)
                throw new ArgumentException($"Not enough arguments in expression: \"{expression}\"");

            var prefix = expression[0];
            var arguments = expression.Remove(0, 1);

            var a = arguments.Split('.');
            if (a.Length > 2)
                throw new ArgumentException($"To many arguments in expression: \"{expression}\"");

            switch (prefix)
            {
                case '+':
                    return new ManipulateImpact(new Reference(arguments), Manipulation.Add);

                case '-':
                    return new ManipulateImpact(new Reference(arguments), Manipulation.Remove);

                case '!':
                    return new ManipulateImpact(new Reference(arguments), Manipulation.Invert);

                case '>':
                    if (a.Length > 1)
                        break;
                    return new FocusImpact(a[0]);

                case '#':
                    return new EventImpact(new Reference(arguments));
            }

            throw new ArgumentException($"Prefix does not match argument in expression: \"{expression}\"");
        }
    }

    public class ManipulateImpact : Impact
    {
        public Manipulation Manipulation { get; }

        public Reference Reference { get; }

        public ManipulateImpact(Reference reference, Manipulation manipulation)
        {
            Reference = reference;
            Manipulation = manipulation;
        }

        public override string ToString()
        {
            switch (Manipulation)
            {
                case Manipulation.Add:
                    return $"+{Reference}";
                case Manipulation.Remove:
                    return $"-{Reference}";
                case Manipulation.Invert:
                    return $"!{Reference}";
                default:
                    return base.ToString();
            }
        }
    }

    public class FocusImpact : Impact
    {
        public string Element { get; }

        public FocusImpact(string element)
        {
            Element = element;
        }

        public override string ToString() => $">{Element}";
    }

    public class EventImpact : Impact
    {
        public Reference Reference { get; }

        public EventImpact(Reference reference)
        {
            Reference = reference;
        }

        public override string ToString() => $"#{Reference}";
    }
}

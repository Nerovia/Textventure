using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Textventure.Components;

namespace Textventure.Engine
{
    public abstract class Engine
    { 
        public virtual void Run()
        {
            Init();
            while (true)
                Update();
        }

        public abstract void Init();

        public abstract void Update();
    }

    public class BasicEngine : Engine
    {
        public enum State
        {
            ClearFrame,
            Describe,
            ListOptions,
            Interact,
            Respond,
        }



        public BasicEngine(World world, Size windowSize)
        {
            World = world;
            World.FocusChanged += OnFocusChanged;
            WindowSize = windowSize;
        }



        public virtual World World { get; }

        public virtual State EngineState { get; set; } = State.ClearFrame;

        public string GameTitle { get; set; } = "Textventure";

        public string ResponsePlaceholder { get; set; } = "What do you do?";

        public string ExitInteractionName { get; set; } = "Back to";



        protected virtual Size WindowSize { get; }

        protected virtual Size TextMargin { get; } = new Size(2, 1); 

        protected virtual Rectangle OuterFrame { get => UI.Frame.Margin(4, 2); }
        
        protected virtual Rectangle DescriptionFrame { get => OuterFrame.Crop(13, VerticalAlignment.Top).Margin(4, 2); }
        
        protected virtual Rectangle OptionsFrame { get => new Rectangle(DescriptionFrame.Left, DescriptionFrame.Bottom, DescriptionFrame.Width, SeparatorFrame.Top - DescriptionFrame.Bottom - 1).Margin(2, 1); }
        
        protected virtual Rectangle SeparatorFrame { get => OuterFrame.Crop(10, VerticalAlignment.Bottom).Crop(1, VerticalAlignment.Top).Margin(5, 0); }
        
        protected virtual Rectangle ResponseFrame { get => new Rectangle(SeparatorFrame.Left, SeparatorFrame.Bottom, SeparatorFrame.Width, OuterFrame.Bottom - SeparatorFrame.Bottom - 2).Margin(2, 1); }

        

        protected string currentDescription;

        protected string currentResponse;

        protected Dictionary<char, IOption> currentOptions;


        
        public override void Init()
        {
            UI.Init(WindowSize);
            UI.Title = GameTitle;
            UI.IsSkipping = false;
            UI.CanSkip = true;
            UI.ShowCursor = false;
            EngineState = State.ClearFrame;
        }

        public override void Update()
        {
            switch (EngineState)
            {
                case State.ClearFrame:
                    DrawScene();
                    UI.Wait(0.3, true);
                    DrawTitle(World.Focus.Name);
                    UI.Wait(0.7, false);
                    EngineState = State.Describe;
                    break;

                case State.Describe:
                    EngineState = State.ListOptions;
                    DrawDescription(World.Focus.Evaluate());
                    UI.Wait(0.7, true);
                    break;

                case State.ListOptions:
                    EngineState = State.Interact;
                    currentOptions = DrawOptions(World.Focus, false, 0.3);
                    DrawResponse($"[q]{ResponsePlaceholder}");
                    break;

                case State.Interact:
                    IOption option;
                    if (currentOptions.TryGetValue(Console.ReadKey(true).KeyChar, out option))
                    {
                        if (option is Interaction)
                        {
                            var response = option.Evaluate();
                            DrawResponse(response);
                            UI.Wait(0.3, true);
                            currentOptions = DrawOptions(World.Focus, true, 0.3);
                        }
                        else if (option is Element)
                        {
                            World.Focus = option as Element;
                            EngineState = State.ClearFrame;
                        }
                    }
                    break;
            }
        }


        protected virtual void OnFocusChanged(object sender, FocusEventArgs args)
        {
            EngineState = State.ClearFrame;
        }


        protected virtual void DrawScene()
        {
            UI.DrawBox(OuterFrame, new BoxOptions()
            {
                outline = BoxOutline.Single,
                fill = false,
            });

            UI.DrawBox(DescriptionFrame, new BoxOptions()
            {
                outline = BoxOutline.Double,
                fill = true,
            });

            UI.Clear(OptionsFrame);
            UI.Clear(ResponseFrame);
            UI.Fill(SeparatorFrame, '─');
        }

        protected virtual void DrawDescription(string description)
        {
            UI.Write(DescriptionFrame.Margin(TextMargin), description, new WriteOptions() 
            { 
                resetSkip = false 
            });
        }

        protected virtual void DrawTitle(string title)
        {
            var titleFrame = OuterFrame.Crop(title.Length + 2, 1, HorizontalAlignment.Center, VerticalAlignment.Top);
            UI.Clear(titleFrame);
            UI.Write(titleFrame.Margin(1, 0), title, new WriteOptions() 
            { 
                showCursor = false, 
                speed = Speed.Normal 
            });
        }

        protected virtual void DrawResponse(string response)
        {
            UI.Clear(ResponseFrame);
            UI.Wait(0.2, true);
            UI.Write(ResponseFrame, response);
        }

        protected virtual Dictionary<char, IOption> DrawOptions(Element focus, bool skip, double interval)
        {
            var options = new Dictionary<char, IOption>();
            var linebreak = $"\n[w]{{{interval}}}";
            if (skip)
                linebreak = "\n";
            var text = "[i]";

            char e = '1';
            foreach (var interaction in focus.GetAvailableInteractions())
            {
                options.Add(e, interaction);
                text += $"[[{e}] {interaction.Name}{linebreak}";
                if (++e > '8')
                    break;
            }

            e = 'a';
            foreach (var item in focus.GetAvailableItems())
            {
                if (e == 'a' && options.Count > 0)
                    text += '\n';

                options.Add(e, item);
                text += $"[[{e}] {item.Name}{linebreak}";
                if (++e > 'h')
                    break;
            }

            e = 'x';
            if (focus.Parent != null)
            {
                if (options.Count > 0)
                    text += '\n';
                options.Add(e, focus.Parent);
                text += $"[[{e}] {ExitInteractionName} { focus.Parent.Name}";
            }

            UI.Write(OptionsFrame, text, new WriteOptions() { showCursor = false });

            return options;
        }
    }
}

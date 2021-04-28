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
            ShowInventory,
            ListItems,
            PickItem,
            Combine,
        }



        public BasicEngine(World world, Size windowSize)
        {
            World = world;
            World.Focus = World.GetReference<Scene>(World.StartKey);
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

        protected virtual Rectangle InventoryItemsFrame { get => Rectangle.FromLTRB(OuterFrame.Left, OuterFrame.Top, OuterFrame.Right, InventoryDescriptionFrame.Top).Margin(6, 3); }

        protected virtual Rectangle InventoryDescriptionFrame { get => OuterFrame.Crop(14, VerticalAlignment.Bottom).Margin(4, 2); }



        protected string currentDescription;

        protected string currentResponse;

        protected Item selectedItem = null;

        protected Dictionary<char, IOption> currentOptions;

        protected Dictionary<char, Item> currentItems;

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
                    var input = Console.ReadKey(true);
                    if (input.KeyChar == 'i')
                    {
                        EngineState = State.ShowInventory;
                    }
                    else if (currentOptions.TryGetValue(input.KeyChar, out option))
                    {
                        if (option is Interaction)
                        {
                            var response = option.Evaluate();
                            DrawResponse(response);
                            UI.Wait(0.3, true);
                            currentOptions = DrawOptions(World.Focus, true, 0.3);
                        }
                        else if (option is Scene)
                        {
                            World.Focus = option as Scene;
                            EngineState = State.ClearFrame;
                        }
                    }
                    break;

                case State.ShowInventory:
                    DrawInventory();
                    UI.Wait(0.3, true);
                    DrawTitle("Inventory");
                    UI.Wait(0.7, false);
                    selectedItem = null;
                    EngineState = State.ListItems;
                    break;

                case State.ListItems:
                    currentItems = ListItems(true, 0.3);
                    EngineState = State.PickItem;
                    break;

                case State.PickItem:
                    var input2 = Console.ReadKey(true);
                    Item item;
                    if (input2.KeyChar == 'x')
                    {
                        EngineState = State.ClearFrame;
                    }
                    else if (currentItems.TryGetValue(input2.KeyChar, out item))
                    {
                        if (selectedItem == item)
                        {
                            UI.Clear(InventoryDescriptionFrame.Margin(2, 1));
                            UI.Write(InventoryDescriptionFrame.Margin(2, 1), $"Combining {item.Name} with ...");
                            EngineState = State.Combine;
                        }
                        else
                        {
                            selectedItem = item;
                            UI.Clear(InventoryDescriptionFrame.Margin(2, 1));
                            UI.Write(InventoryDescriptionFrame.Margin(2, 1), item.Evaluate());
                        }
                    }
                    break;

                case State.Combine:
                    var input3 = Console.ReadKey(true);
                    Item item3 = null;
                    ICombinable combinable = null;
                    if (input3.KeyChar == 'x')
                    {
                        combinable = World.Focus;
                    }
                    else if (currentItems.TryGetValue(input3.KeyChar, out item3))
                    {
                        if (item3 == selectedItem)
                        {
                            UI.Clear(InventoryDescriptionFrame.Margin(2, 1));
                            UI.Write(InventoryDescriptionFrame.Margin(2, 1), item3.Evaluate());
                            EngineState = State.PickItem;
                            break;
                        }
                        combinable = item3;
                    }

                    if (combinable != null)
                    {
                        var combination = combinable.GetAvailableCombinationWith(selectedItem);
                        if (combination != null)
                        {
                            UI.Clear(InventoryDescriptionFrame.Margin(2, 1));
                            UI.Write(InventoryDescriptionFrame.Margin(2, 1), combination.Evaluate());
                            EngineState = State.ListItems;
                        }
                        else
                        {
                            UI.Clear(InventoryDescriptionFrame.Margin(2, 1));
                            UI.Write(InventoryDescriptionFrame.Margin(2, 1), "Nothing happens...");
                        }
                        EngineState = State.ListItems;
                    }
                    break;
            }
        }


        protected virtual void OnFocusChanged(object sender, FocusEventArgs args)
        {
            EngineState = State.ClearFrame;
        }

        protected virtual void DrawInventory()
        {
            UI.DrawBox(OuterFrame, new BoxOptions()
            {
                outline = BoxOutline.Single,
                fill = true,
            });

            UI.DrawBox(InventoryDescriptionFrame, new BoxOptions()
            {
                outline = BoxOutline.Double,
                fill = false,
            });
        }

        protected virtual Dictionary<char, Item> ListItems(bool skip, double interval)
        {
            var options = new Dictionary<char, Item>();
            var linebreak = $"\n[w]{{{interval}}}";
            if (skip)
                linebreak = "\n";
            var text = "[i]";

            char e = '1';
            foreach (var items in World.Player.GetAvailableItems())
            {
                options.Add(e, items);
                text += $"[[{e}] {items.Name}{linebreak}";
                if (++e > '8')
                    break;
            }

            text += $"[[X] {ExitInteractionName} {World.Focus}";

            UI.Write(InventoryItemsFrame, text, new WriteOptions() { showCursor = false });

            return options;
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

        protected virtual Dictionary<char, IOption> DrawOptions(Scene focus, bool skip, double interval)
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
            foreach (var item in focus.GetAvailableSubScenes())
            {
                if (e == 'a' && options.Count > 0)
                    text += '\n';

                options.Add(e, item);
                text += $"[[{e}] {item.Name}{linebreak}";
                if (++e > 'h')
                    break;
            }

            e = 'x';
            if (focus.Parent is Scene)
            {
                var parent = focus.Parent as Scene;
                if (options.Count > 0)
                    text += '\n';
                options.Add(e, parent);
                text += $"[[{e}] {ExitInteractionName} {parent.Name}";
            }

            UI.Write(OptionsFrame, text, new WriteOptions() { showCursor = false });

            return options;
        }
    }
}

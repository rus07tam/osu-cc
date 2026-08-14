using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osucc.Client;
using osucc.Core;
using osucc.Patches;
using osuTK;
using osuTK.Graphics;
using System;
using System.Linq;

namespace osucc.UI.Overlays
{
    public partial class KeyHistoryOverlayComponent : Container
    {
        private readonly FillFlowContainer<KeyHistoryItem> flow;

        public KeyHistoryOverlayComponent()
        {
            RelativeSizeAxes = Axes.Both;
            AlwaysPresent = true;
            Depth = float.MinValue;

            Add(flow = new FillFlowContainer<KeyHistoryItem>
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 4),
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ClientConfig.KeyHistoryMode.BindValueChanged(onModeChanged, true);
        }

        private void onModeChanged(ValueChangedEvent<KeyHistoryOverlayMode> e)
        {
            var mode = e.NewValue;

            if (mode == KeyHistoryOverlayMode.Disabled)
            {
                flow.Clear();
                flow.FadeOut(200);
                InputManagerHandlePatch.OnInputEvent -= handleInputEvent;
            }
            else
            {
                flow.FadeIn(200);

                switch (mode)
                {
                    case KeyHistoryOverlayMode.TopLeft:
                        flow.Anchor = Anchor.TopLeft;
                        flow.Origin = Anchor.TopLeft;
                        flow.Margin = new MarginPadding(20) { Top = 60 };
                        break;
                    case KeyHistoryOverlayMode.TopRight:
                        flow.Anchor = Anchor.TopRight;
                        flow.Origin = Anchor.TopRight;
                        flow.Margin = new MarginPadding(20) { Top = 60 };
                        break;
                    case KeyHistoryOverlayMode.BottomLeft:
                        flow.Anchor = Anchor.BottomLeft;
                        flow.Origin = Anchor.BottomLeft;
                        flow.Margin = new MarginPadding(20);
                        break;
                    case KeyHistoryOverlayMode.BottomRight:
                        flow.Anchor = Anchor.BottomRight;
                        flow.Origin = Anchor.BottomRight;
                        flow.Margin = new MarginPadding(20);
                        break;
                }

                InputManagerHandlePatch.OnInputEvent -= handleInputEvent;
                InputManagerHandlePatch.OnInputEvent += handleInputEvent;
            }
        }

        private void handleInputEvent(UIEvent e)
        {
            if (e is KeyDownEvent keyDown)
            {
                Schedule(() =>
                {
                    if (ClientConfig.KeyHistoryMode.Value == KeyHistoryOverlayMode.Disabled)
                        return;

                    var item = new KeyHistoryItem(keyDown.Key);
                    flow.Add(item);

                    while (flow.Count > 8)
                    {
                        flow.Remove(flow[0], true);
                    }
                });
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            InputManagerHandlePatch.OnInputEvent -= handleInputEvent;
        }

        private sealed partial class KeyHistoryItem : Container
        {
            public KeyHistoryItem(osuTK.Input.Key key)
            {
                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 4;
                BorderThickness = 1.5f;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.6f,
                    },
                    new SpriteText
                    {
                        Text = getKeyName(key),
                        Font = OsuFont.Default.With(size: 14, weight: FontWeight.Bold),
                        Padding = new MarginPadding { Horizontal = 8, Vertical = 4 },
                        Colour = Color4.White,
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                BorderColour = colours.Pink;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                this.FadeInFromZero(100)
                    .Delay(1200)
                    .FadeOut(300)
                    .Expire();
            }

            private static string getKeyName(osuTK.Input.Key key)
            {
                switch (key)
                {
                    case osuTK.Input.Key.LShift: return "LShift";
                    case osuTK.Input.Key.RShift: return "RShift";
                    case osuTK.Input.Key.LControl: return "LCtrl";
                    case osuTK.Input.Key.RControl: return "RCtrl";
                    case osuTK.Input.Key.LAlt: return "LAlt";
                    case osuTK.Input.Key.RAlt: return "RAlt";
                    case osuTK.Input.Key.WinLeft: return "Win";
                    case osuTK.Input.Key.WinRight: return "Win";
                    case osuTK.Input.Key.Escape: return "Esc";
                    case osuTK.Input.Key.BackSpace: return "Back";
                    case osuTK.Input.Key.Space: return "Space";
                    case osuTK.Input.Key.Left: return "←";
                    case osuTK.Input.Key.Right: return "→";
                    case osuTK.Input.Key.Up: return "↑";
                    case osuTK.Input.Key.Down: return "↓";
                    default:
                        string name = key.ToString();
                        if (name.StartsWith("Number", StringComparison.Ordinal))
                            return name.Replace("Number", "");
                        return name;
                }
            }
        }
    }
}

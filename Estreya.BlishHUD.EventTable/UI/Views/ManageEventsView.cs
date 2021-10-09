﻿namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Content;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ManageEventsView : View
    {
        private static readonly Logger Logger = Logger.GetLogger<ManageEventsView>();

        public FlowPanel FlowPanel { get; private set; }

        private IEnumerable<EventCategory> EventCategories { get; set; }
        private List<SettingEntry<bool>> EventSettings { get; set; }

        public ManageEventsView(IEnumerable<EventCategory> categories, List<SettingEntry<bool>> settings)
        {
            this.EventCategories = categories;
            this.EventSettings = settings;
        }

        private void UpdateToggleButton(GlowButton button)
        {
            button.Icon = button.Checked ? GameService.Content.GetTexture("minus") : GameService.Content.GetTexture("plus"); // TODO: Own icon
        }

        protected override void Build(Container buildPanel)
        {
            this.FlowPanel = new FlowPanel
            {
                Width = buildPanel.Width,
                Height = buildPanel.Height,
                FlowDirection = ControlFlowDirection.LeftToRight,
                Top = 0,
                CanScroll = true,
                //ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(Panel.MenuStandard.ControlOffset.X, Panel.MenuStandard.ControlOffset.Y),
                Parent = buildPanel
            };
            Rectangle contentRegion = FlowPanel.ContentRegion;

            Panel eventCategoriesPanel = new Panel();
            eventCategoriesPanel.Title = "Event Categories";
            eventCategoriesPanel.Parent = FlowPanel;
            eventCategoriesPanel.CanScroll = true;
            eventCategoriesPanel.ShowBorder = true;
            eventCategoriesPanel.Size = Panel.MenuStandard.Size - new Point(0, Panel.MenuStandard.ControlOffset.Y);
            eventCategoriesPanel.Location = Panel.MenuStandard.PanelOffset;
            Menu eventCategories = new Menu();
            eventCategories.Parent = eventCategoriesPanel;
            eventCategories.Size = eventCategoriesPanel.ContentRegion.Size;
            eventCategories.MenuItemHeight = 40;

            FlowPanel eventPanel = new FlowPanel();
            eventPanel.FlowDirection = ControlFlowDirection.LeftToRight;
            eventPanel.CanScroll = true;
            eventPanel.ShowBorder = true;
            eventPanel.Parent = FlowPanel;
            eventPanel.Location = new Point(0, contentRegion.Y);
            eventPanel.Size = new Point(contentRegion.Width - (eventCategoriesPanel.Location.X + eventCategoriesPanel.Width) - Panel.MenuStandard.PanelOffset.X, contentRegion.Height - (int)FlowPanel.OuterControlPadding.Y);

            Dictionary<string, MenuItem> menus = new Dictionary<string, MenuItem>();

            MenuItem allEvents = eventCategories.AddMenuItem("All Events");
            allEvents.Select();
            menus.Add(nameof(allEvents), allEvents);

            foreach (EventCategory category in EventCategories.GroupBy(ec => ec.Name).Select(ec => ec.First()))
            {
                menus.Add(category.Key, eventCategories.AddMenuItem(category.Name));
            }

            menus.ToList().ForEach(menuItemPair => menuItemPair.Value.Click += (s, e) =>
            {
                MenuItem menuItem = s as MenuItem;
                eventPanel.FilterChildren<DetailsButton>(detailsButton =>
                {
                    IEnumerable<EventCategory> categories = EventCategories.Where(ec => ec.Events.Any(ev => ev.Name == detailsButton.Text));
                    return menuItem == menus[nameof(allEvents)] || categories.Any(ec => ec.Name == menuItem.Text);
                });
            });

            foreach (EventCategory category in EventCategories)
            {
                IEnumerable<Event> events = category.ShowCombined ? category.Events.GroupBy(e => e.Name).Select(eg => eg.First()) : category.Events;
                foreach (Event e in events)
                {
                    IEnumerable<SettingEntry<bool>> settings = this.EventSettings.FindAll(eventSetting => eventSetting.EntryKey.Contains(e.Name));

                    SettingEntry<bool> setting = settings.First();
                    bool enabled = setting.Value;

                    AsyncTexture2D icon = EventTableModule.ModuleInstance.ContentsManager.GetRenderIcon(e.Icon);

                    var button = new DetailsButton()
                    {
                        Parent = eventPanel,
                        Text = e.Name,
                        Icon = icon,
                        ShowToggleButton = true,
                        FillColor = Color.LightBlue,
                        //Size = new Point((events.ContentRegion.Size.X - Panel.ControlStandard.Size.X) / 2, events.ContentRegion.Size.X - Panel.ControlStandard.Size.X)
                    };

                    if (!string.IsNullOrWhiteSpace(e.Waypoint))
                    {
                        var waypointButton = new GlowButton()
                        {
                            Parent = button,
                            ToggleGlow = false,
                            Tooltip = new Tooltip(new TooltipView("Waypoint", "Click to Copy", icon: "waypoint")),
                            Icon = GameService.Content.GetTexture("waypoint") // TODO: Own icon
                        };

                        waypointButton.Click += (s, eventArgs) =>
                        {
                            e.CopyWaypoint();
                        };
                    }

                    if (!string.IsNullOrWhiteSpace(e.Wiki))
                    {
                        var wikiButton = new GlowButton()
                        {
                            Parent = button,
                            ToggleGlow = false,
                            Tooltip = new Tooltip(new TooltipView("Wiki", "Click to Open", icon: "102530")),
                            Icon = GameService.Content.GetTexture("102530") // TODO: Own icon
                        };

                        wikiButton.Click += (s, eventArgs) =>
                        {
                            e.OpenWiki();
                        };
                    }

                    var toggleButton = new GlowButton()
                    {
                        Parent = button,
                        Checked = enabled,
                        ToggleGlow = false
                    };

                    UpdateToggleButton(toggleButton);

                    toggleButton.Click += (s, eventArgs) =>
                    {
                        if (setting != null)
                        {
                            setting.Value = !setting.Value;
                            toggleButton.Checked = setting.Value;
                            settings.Where(x => x.EntryKey != setting.EntryKey).ToList().ForEach(x => x.Value = setting.Value);
                            UpdateToggleButton(toggleButton);
                        }
                    };
                }
            }
        }
    }
}
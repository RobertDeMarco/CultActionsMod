// Mod: adds extra cult actions to the sidebar.
//   GatherScientificResearch -> ScientificResearch
//   OfferBrain               -> PreservedBrain
// To add another action later: add a row to the XML, then add a line to
// each of the three switch statements at the bottom of this file.

using Arcen.HotM.Core;
using Arcen.Universal;

namespace CultActions.ExternalVis
{
    public class Sidebar_CultActionsResearch :
        ISidebarBasicItemImplementation,
        IArcenExternalClassNothingSpecialToManageSingleton,
        IArcenExternalClassRoot
    {
        public void Sidebar_GetOrSetUIData(
            UISidebarBasicItem Item,
            ImageButtonAbstractBase ImageController,
            ButtonAbstractBase ButtonController,
            ArcenUI_Element Element,
            UIAction Action,
            ref UIActionData ExtraData)
        {
            // Same UIAction offsets as the original Sidebar_CultActionsItems.
            switch ((int)Action - 2)
            {
                case 0: // tooltip
                {
                    if (ButtonController != null)
                        break;
                    NovelTooltipBuffer instance = NovelTooltipBuffer.Instance;
                    if (!instance.TryStartBasicTooltip(TooltipID.Create(Item), Element, (SideClamp)1,
                            (TooltipNovelWidth)0, (TooltipExtraText)0, (TooltipExtraRules)1, (TooltipArrowSide)0, false))
                        break;
                    instance.TitleUpperLeft.AddRaw(Item.GetDisplayNameForSidebar());
                    string hotkey = Item.GetHotkeyToDisplayAsToggleForSidebar();
                    if (hotkey.Length > 0)
                        instance.TitleLowerLeft.AddTooltipHotkeySecondLine(hotkey);
                    instance.Main.StartColor(ColorTheme.NarrativeColor);
                    WriteSidebarTooltip(Item, instance.Main);
                    instance.Icon = Item.Icon;
                    break;
                }
                case 2: // click
                    HandleClick(Item, ExtraData.MouseInput);
                    break;
                case 5:
                    ExtraData.Buffer.AddRaw(Item.GetDisplayName());
                    break;
                case 6: // sidebar entry text + icon
                {
                    ExtraData.Image.SetSpriteIfNeeded_Simple(Item.GetIcon()?.GetSpriteForUI());
                    ExtraData.SubTextsWrapper[0].Text.StartWritingToBuffer().AddRaw(Item.GetDisplayName());
                    ExtraData.SubTextsWrapper[0].Text.FinishWritingToBuffer();
                    if (ExtraData.SubTextsWrapper.Length <= 1)
                        break;
                    ArcenDoubleCharacterBuffer buffer = ExtraData.SubTextsWrapper[1].Text.StartWritingToBuffer();
                    WriteSidebarSecondLine(Item, buffer);
                    ExtraData.SubTextsWrapper[1].Text.FinishWritingToBuffer();
                    break;
                }
            }
        }

        public SidebarItemType GetItemType(UISidebarBasicItem Item)
        {
            return (SidebarItemType)0; // normal clickable entry
        }

        public MouseHandlingResult HandleClick(UISidebarBasicItem Item, MouseHandlingInput input)
        {
            int cost = GetLoyaltyCost(Item);
            ResourceType resource = GetResource(Item);
            if (cost <= 0 || resource == null || cost > ResourceRefs.CultLoyalty.Current)
            {
                ParticleSoundRefs.BlockedSound.DGD.PlaySoundOnlyAtCamera();
                return (MouseHandlingResult)0;
            }

            IntRange range = GetGainRange(Item);
            int amount = range.Min >= range.Max
                ? range.Max
                : Engine_Universal.PermanentQualityRandom.NextInclus(range.Min, range.Max);
            if (amount < 1)
                amount = 1;

            ResourceRefs.CultLoyalty.AlterCurrent_Named(-cost, string.Empty, (ResourceAddRule)1);
            resource.AlterCurrent_Named(amount, string.Empty, (ResourceAddRule)1);

            ArcenNotes.SendSimpleNoteToGameOnly(300,
                NoteInstructionTable.Instance.GetRowByID("GainedResource"),
                (NoteStyle)4, resource.ID, amount, 0L, 0L, SimCommon.Turn + 100);

            ParticleSoundRefs.CultGather.DGD.Play();
            return (MouseHandlingResult)0;
        }

        public void WriteSidebarTooltip(UISidebarBasicItem Item, ArcenDoubleCharacterBuffer buffer)
        {
            if (!Item.GetDescription().IsEmpty())
                buffer.AddRaw(Item.GetDescription()).Line();

            int cost = GetLoyaltyCost(Item);
            if (cost <= 0)
                return;
            ResourceType resource = GetResource(Item);
            bool affordable = resource != null && cost <= ResourceRefs.CultLoyalty.Current;

            buffer.StartStyleLineHeightA();
            buffer.AddBoldLangAndAfterLineItemHeader("Cost", ColorTheme.DataLabelWhite)
                .AddSprite(ResourceRefs.CultLoyalty.Icon, (A5SpriteStyle)1, ResourceRefs.CultLoyalty.IconColorHex)
                .AddThousandsWhole(cost, affordable ? ColorTheme.DataGood : ColorTheme.DataProblem)
                .Line();

            if (resource != null)
            {
                IntRange range = GetGainRange(Item);
                buffer.AddBoldLangAndAfterLineItemHeader("Gain", ColorTheme.DataLabelWhite)
                    .AddSprite(resource.Icon, (A5SpriteStyle)1, resource.IconColorHex);
                if (range.Min < range.Max)
                    buffer.AddFormat2("MinMaxRange", range.Min.ToStringThousandsWhole(),
                        range.Max.ToStringThousandsWhole(), ColorTheme.DataGood);
                else
                    buffer.AddThousandsWhole(range.Max, ColorTheme.DataGood);
                buffer.Space1x().AddFormat1("ParentheticalAvailable",
                    resource.Current.ToStringThousandsWhole(),
                    resource.Current > 0L ? ColorTheme.CategorySelectedBlue : ColorTheme.Gray);
            }
            buffer.EndLineHeight();
        }

        public void WriteSidebarSecondLine(UISidebarBasicItem Item, ArcenDoubleCharacterBuffer buffer)
        {
            int cost = GetLoyaltyCost(Item);
            if (cost <= 0)
                return;
            bool affordable = GetResource(Item) != null && cost <= ResourceRefs.CultLoyalty.Current;
            buffer.AddSprite(ResourceRefs.CultLoyalty.Icon, (A5SpriteStyle)1, ResourceRefs.CultLoyalty.IconColorHex)
                .AddThousandsWhole(cost, affordable ? ColorTheme.DataGood : ColorTheme.DataProblem);
        }

        // =====================================================================
        // Per-action tuning. The case labels are the XML row ids.
        // =====================================================================

        private int GetLoyaltyCost(UISidebarBasicItem Item)
        {
            switch (Item.ID)
            {
                case "StealthResearch": return 10000;
                case "OfferBrain": return 1200;
                default: return -1;
            }
        }

        private IntRange GetGainRange(UISidebarBasicItem Item)
        {
            switch (Item.ID)
            {
                case "StealthResearch": return new IntRange(400, 600);
                case "OfferBrain": return new IntRange(1500, 3200);
                default: return new IntRange(-1, -1);
            }
        }

        // The resource IDs must match the ResourceType row ids in the game's XML exactly.
        private static ResourceType GetResource(UISidebarBasicItem Item)
        {
            switch (Item.ID)
            {
                case "StealthResearch": return LookupResource("ScientificResearch");
                case "OfferBrain": return LookupResource("PreservedBrain");
                default: return null;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<string, ResourceType> resourceCache =
            new System.Collections.Generic.Dictionary<string, ResourceType>();

        private static ResourceType LookupResource(string resourceID)
        {
            lock (resourceCache)
            {
                if (resourceCache.TryGetValue(resourceID, out ResourceType cached) && cached != null)
                    return cached;
                ResourceType found = ResourceTypeTable.Instance.GetRowByID(resourceID);
                resourceCache[resourceID] = found;
                return found;
            }
        }
    }
}

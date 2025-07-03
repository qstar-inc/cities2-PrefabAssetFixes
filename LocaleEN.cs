using System.Collections.Generic;
using Colossal;

namespace PrefabAssetFixes
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts
        )
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), Mod.Name },
                { m_Setting.GetOptionTabLocaleID(Setting.OptionsTab), "Options" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), Setting.AboutTab },
                { m_Setting.GetOptionGroupLocaleID(Setting.VisualGroup), Setting.VisualGroup },
                {
                    m_Setting.GetOptionGroupLocaleID(Setting.FunctionalGroup),
                    Setting.FunctionalGroup
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Prison)), "Prison's Van Count" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.Prison)),
                    $"Change the vanilla Prison's Prison Van capacity from 10 to 20 to fill up all the service vehicle parkings.\r\nRequires replopping of the vanilla Prison for this change to take effect."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PrisonVan)), "Prison Van Spawn" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.PrisonVan)),
                    $"Change the vanilla Prison Van's size class from Medium to Large so it can spawn from the prisons.\r\nRequires replopping of the prison assets for this change to take effect."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Storage)), "Storage Section" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.Storage)),
                    $"Fix Storage Section not being shown for service buildings that require resources.\r\nRequires reloading the save for this change to take effect."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Recycling)), "Recycling System" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.Recycling)),
                    $"Fix any Recycling buildings not being able to sell their products.\r\nRequires reloading the save for this change to take effect."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.Hospital)),
                    "Hospital Service Parking"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.Hospital)),
                    $"Change the vanilla Hospitals' service vehicle parking count from 10 to 30.\r\nInstant change."
                },
                {
                    m_Setting.GetOptionLabelLocaleID(nameof(Setting.HoveringPoles)),
                    "Hovering Poles & Signs"
                },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.HoveringPoles)),
                    $"Fix hovering poles and signs present in China and US South West Region Packs.\r\nInstant change."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModState)), "Mod State" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModState)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod Name" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Mod Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AuthorText)), "Author" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AuthorText)), "" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.BMaCLink)), "Buy Me a Coffee" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.BMaCLink)),
                    "Support the author."
                },
            };
        }

        public void Unload() { }
    }
}

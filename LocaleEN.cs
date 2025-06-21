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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.Prison)), "Prison" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.Prison)),
                    $"Change the vanilla Prison's Prison Van capacity from 10 to 20 to fill up all the service vehicle parkings."
                },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.PrisonVan)), "Prison Van" },
                {
                    m_Setting.GetOptionDescLocaleID(nameof(Setting.PrisonVan)),
                    $"Change the vanilla Prison Van's size class from Medium to Large so it can spawn from the prisons."
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

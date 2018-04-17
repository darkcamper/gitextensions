﻿using System.ComponentModel.Composition;
using System.Windows.Forms;
using GitUIPluginInterfaces;
using ResourceManager;

namespace ReleaseNotesGenerator
{
    [Export(typeof(IGitPlugin))]
    public class ReleaseNotesGeneratorPlugin : GitPluginBase
    {
        public ReleaseNotesGeneratorPlugin()
        {
            SetNameAndDescription("Release Notes Generator");
            Translate();
        }

        public override bool Execute(GitUIEventArgs gitUiCommands)
        {
            using (var form = new ReleaseNotesGeneratorForm(gitUiCommands))
            {
                if (form.ShowDialog(gitUiCommands.OwnerForm) == DialogResult.OK)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

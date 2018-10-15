﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using GitUI.Properties;
using JetBrains.Annotations;
using ResourceManager;

namespace GitUI.UserControls.RevisionGrid
{
    public sealed class CopyContextMenuItem : ToolStripMenuItem
    {
        private readonly TranslationString _copyToClipboardText = new TranslationString("&Copy to clipboard");
        [CanBeNull] private Func<IReadOnlyList<GitRevision>> _revisionFunc;
        private uint _itemNumber;

        public CopyContextMenuItem()
        {
            Image = Images.CopyToClipboard;
            Text = _copyToClipboardText.Text;

            // Create a dummy menu, so that the shortcut keys work.
            OnDropDownOpening(null, null);

            DropDownOpening += OnDropDownOpening;
        }

        public void SetRevisionFunc(Func<IReadOnlyList<GitRevision>> revisionFunc)
        {
            _revisionFunc = revisionFunc;
        }

        private void AddItem(string displayText, Func<GitRevision, string> extractRevisionText, Image image, char? hotkey, Keys shortcutKeys = Keys.None)
        {
            if (hotkey.HasValue)
            {
                int position = displayText.IndexOf(hotkey.Value.ToString(), StringComparison.InvariantCultureIgnoreCase);
                if (position >= 0)
                {
                    displayText = displayText.Insert(position, "&");
                }
            }
            else
            {
                displayText = PrependItemNumber(displayText);
            }

            var texts = ExtractRevisionTexts(extractRevisionText);
            if (texts != null)
            {
                displayText += ":   " + texts.Select(t => t.SubstringUntil('\n')).Join(", ").ShortenTo(40);
            }

            var item = new ToolStripMenuItem
            {
                Text = displayText,
                ShortcutKeys = shortcutKeys,
                ShowShortcutKeys = true,
                Image = image
            };
            item.Click += delegate
            {
                var textToCopy = ExtractRevisionTexts(extractRevisionText);
                if (textToCopy == null)
                {
                    return;
                }

                Clipboard.SetText(textToCopy.Join("\n"));
            };

            DropDownItems.Add(item);
        }

        private IEnumerable<string> ExtractRevisionTexts(Func<GitRevision, string> extractRevisionText)
        {
            if (extractRevisionText == null)
            {
                return null;
            }

            var gitRevisions = _revisionFunc?.Invoke();
            if (gitRevisions == null || gitRevisions.Count == 0)
            {
                return null;
            }

            return gitRevisions.Select(extractRevisionText).Distinct();
        }

        private void OnDropDownOpening(object sender, EventArgs e)
        {
            var revisions = _revisionFunc?.Invoke();
            if (revisions == null || revisions.Count == 0)
            {
                if (sender == null)
                {
                    // create the initial dummy menu on a dummy revision
                    revisions = new List<GitRevision> { new GitRevision(GitUIPluginInterfaces.ObjectId.WorkTreeId) };
                }
                else
                {
                    HideDropDown();
                    return;
                }
            }

            DropDownItems.Clear();

            List<string> branchNames = new List<string>();
            List<string> tagNames = new List<string>();
            foreach (var revision in revisions)
            {
                var refLists = new GitRefListsForRevision(revision);
                branchNames.AddRange(refLists.GetAllBranchNames());
                tagNames.AddRange(refLists.GetAllTagNames());
            }

            _itemNumber = 0;

            // Add items for branches
            if (branchNames.Any())
            {
                var caption = new ToolStripMenuItem { Text = Strings.Branches };
                MenuUtil.SetAsCaptionMenuItem(caption, Owner);
                DropDownItems.Add(caption);

                foreach (var name in branchNames)
                {
                    AddItem(name, extractRevisionText: null, Images.Branch, hotkey: null);
                }

                DropDownItems.Add(new ToolStripSeparator());
            }

            // Add items for tags
            if (tagNames.Any())
            {
                var caption = new ToolStripMenuItem { Text = Strings.Tags };
                MenuUtil.SetAsCaptionMenuItem(caption, Owner);
                DropDownItems.Add(caption);

                foreach (var name in tagNames)
                {
                    AddItem(name, extractRevisionText: null, Images.Tag, hotkey: null);
                }

                DropDownItems.Add(new ToolStripSeparator());
            }

            // Add other items
            int count = revisions.Count();
            AddItem(Strings.GetCommitHash(count), r => r.Guid, Images.CommitId, 'C', Keys.Control | Keys.C);
            AddItem(Strings.GetMessage(count), r => r.Body ?? r.Subject, Images.Message, 'M');
            AddItem(Strings.GetAuthor(count), r => r.Author, Images.Author, 'A');

            if (count == 1 && revisions.First().AuthorDate == revisions.First().CommitDate)
            {
                AddItem(Strings.Date, r => r.AuthorDate.ToString(), Images.Date, 'D');
            }
            else
            {
                AddItem(Strings.GetAuthorDate(count), r => r.AuthorDate.ToString(), Images.Date, 'T');
                AddItem(Strings.GetCommitDate(count), r => r.CommitDate.ToString(), Images.Date, 'D');
            }
        }

        private string PrependItemNumber(string name)
        {
            return ++_itemNumber > 10 ? name : "&" + (_itemNumber % 10) + ":   " + name;
        }
    }
}
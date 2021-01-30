using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using BnsDatTool.lib.UndoRedo;
using BnS_Multitool.Properties;

namespace BnsDatTool.lib
{
    public class TranslateFileBl
    {
        private IEnumerable<TranslatableItem> _elements;

        public string OpenedFilePath { get; private set; }
        public UndoRedoManager UndoRedoManager { get; private set; }
        public bool HasChanges { get; private set; }

        public void Load(string path)
        {
            _elements = TranslateFileDal.Open(path);
            OpenedFilePath = path;
            HasChanges = false;
            UndoRedoManager = new UndoRedoManager();
        }

        public void Save()
        {
            TranslateFileDal.Save(OpenedFilePath, _elements);
            HasChanges = false;
        }

        public void Save(string path)
        {
            TranslateFileDal.Save(path, _elements);
            HasChanges = false;
        }

        public IEnumerable<TranslatableItem> GetElements()
        {
            return _elements.ToList();
        }
        public IEnumerable<TranslatableItem> GetUnTranElements(string type)
        {
            switch (type)
            {
                case "KR Text":
                    return _elements.Where(u => u.Translate.Any(CharExtension.IsKoreaChar)).ToList();
                case "JP Text":
                    return _elements.Where(u => u.Translate.Any(CharExtension.IsJapanChar)).ToList();
                case "TH Text":
                    return _elements.Where(u => u.Translate.Any(CharExtension.IsThaiChar)).ToList();
                default:
                    return _elements.Where(u => u.Translate.Any(CharExtension.IsChineseChar)).ToList();
            }

        }
        public IEnumerable<TranslatableItem> GetGTranElements()
        {
            return _elements.Where(u => u.Type == "gg").ToList();
        }
        public IEnumerable<TranslatableItem> GetElements(FilterCriteria criteria)
        {
            Expression<Func<TranslatableItem, bool>> matcherExpression;
            Func<TranslatableItem, string> fieldGetter;

            switch (criteria.Field)
            {
                case TranslatableItem.Fields.Alias:
                    fieldGetter = item => item.Alias;
                    break;

                case TranslatableItem.Fields.Original:
                    fieldGetter = item => item.Original;
                    break;

                case TranslatableItem.Fields.Translate:
                    fieldGetter = item => item.Translate;
                    break;

                default:
                    throw new ArgumentException(Resources.AutoIdSearchNotSupported);
            }

            if (criteria.IsRegex)
            {
                Regex regex = CreateRegex(criteria.Value, criteria.IsIgnoreCase);
                matcherExpression = item => regex.IsMatch(fieldGetter(item));
            }
            else
            {
                StringComparison comparisonType = GetComparison(criteria.IsIgnoreCase);
                matcherExpression = item => Compare(fieldGetter(item), criteria.Value, comparisonType);
            }

            return _elements.AsParallel().AsOrdered().Where(matcherExpression.Compile()).ToList();
        }

        public IEnumerable<TranslatableItem> ReplaceTranslate(ReplaceCriteria criteria)
        {
            FilterCriteria searchCriteria = new FilterCriteria(criteria.Pattern, TranslatableItem.Fields.Translate,
                criteria.IsRegex, criteria.IsIgnoreCase);

            List<TranslatableItem> result = GetElements(searchCriteria).ToList();
            ConcurrentBag<EditableElement> elements = new ConcurrentBag<EditableElement>();
            Expression<Func<TranslatableItem, string>> replaceExpression;

            if (criteria.IsRegex)
            {
                Regex regex = CreateRegex(criteria.Pattern, criteria.IsIgnoreCase);
                replaceExpression = item => regex.Replace(item.Translate, criteria.Replacement);
            }
            else
            {
                StringComparison comparisonType = GetComparison(criteria.IsIgnoreCase);
                replaceExpression = item => Replace(item.Translate, criteria.Pattern, criteria.Replacement, comparisonType);
            }
            Func<TranslatableItem, string> replaceFunction = replaceExpression.Compile();

            result.AsParallel().ForAll(item =>
            {
                string newTranslate = replaceFunction(item);
                elements.Add(new EditableElement(item, newTranslate));
                item.UpdateTranslate(newTranslate, false);
            });

            UndoRedoManager.Add(elements.ToList());
            HasChanges = true;
            return result;
        }

        public void ElementUpdated(TranslatableItem element, string newValue, bool isGtran)
        {
            if (element.Translate == newValue)
                return;

            UndoRedoManager.Add(new EditableElement(element, newValue));
            element.UpdateTranslate(newValue, isGtran);
            HasChanges = true;
        }
        public string ElementReset(TranslatableItem element)
        {
            return element.ResetTranslate();
        }
        #region Helpers

        private static bool Compare(string strA, string strB, StringComparison comparisonType)
        {
            return strA.IndexOf(strB, comparisonType) >= 0;
        }

        private static Regex CreateRegex(string pattern, bool isIgnoreCase)
        {
            RegexOptions options = RegexOptions.Compiled | RegexOptions.Singleline;
            if (isIgnoreCase)
                options |= RegexOptions.IgnoreCase;

            return new Regex(pattern, options);
        }

        private static StringComparison GetComparison(bool isIgnoreCase)
        {
            return isIgnoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture;
        }

        private static string Replace(string original, string pattern, string replacement, StringComparison comparisonType)
        {
            if (pattern == replacement)
                return original;

            int position = original.IndexOf(pattern, comparisonType);

            while (position != -1)
            {
                original = original.Remove(position, pattern.Length).Insert(position, replacement);
                position += replacement.Length;
                if (position > original.Length)
                    break;
                position = original.IndexOf(pattern, position, comparisonType);
            }

            return original;
        }

        #endregion

    }

}

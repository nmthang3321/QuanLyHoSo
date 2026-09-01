namespace QuanLyHoSo.Models
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public sealed class AreaSelectionOption
    {
        public string DisplayName { get; set; }
        public string FilterValue { get; set; }
        public string GroupName { get; set; }
        public string ToolTip { get; set; }
        public bool IsGroup { get; set; }
        public bool IsSelectable { get; set; } = true;
        public ObservableCollection<AreaSelectionOption> Children { get; } = new ObservableCollection<AreaSelectionOption>();
        public bool HasChildren => Children.Count > 0;

        public override string ToString()
        {
            return DisplayName ?? string.Empty;
        }
    }

    public static class AreaSelectionOptions
    {
        public const string AllAreas = "Tất cả";
        public const string CommuneGroup = "Cấp xã";
        public const string ProvinceGroup = "Cấp tỉnh";
        public const string MinistryGroup = "Cấp bộ";
        public const string ProvincialPoliceGroup = "Công an tỉnh";
        public const string ExternalPoliceGroup = "Đơn vị trong ngành ngoại tỉnh";

        private static readonly string[] ProvinceUnits =
        {
            "Tỉnh ủy An Giang",
            "Ủy ban nhân dân tỉnh",
            "Ban Nội chính Tỉnh ủy",
            "Thanh tra tỉnh"
        };

        private static readonly string[] MinistryUnits =
        {
            "C01",
            "C02",
            "C03",
            "C04",
            "X05",
            "X06"
        };

        private static readonly string[] ProvincialPoliceUnits =
        {
            "PC02",
            "PC03",
            "PC04",
            "PX05",
            "PX06",
            "Đơn vị khác trong tỉnh"
        };

        private static readonly string[] ExternalPoliceUnits =
        {
            ExternalPoliceGroup
        };

        public static ObservableCollection<AreaSelectionOption> Build(IEnumerable<string> areaNames, bool includeGroupRows, bool groupRowsSelectable)
        {
            var source = (areaNames ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var result = new ObservableCollection<AreaSelectionOption>();
            if (source.RemoveAll(name => string.Equals(name, AllAreas, StringComparison.CurrentCultureIgnoreCase)) > 0)
            {
                result.Add(new AreaSelectionOption
                {
                    DisplayName = AllAreas,
                    FilterValue = AllAreas,
                    ToolTip = AllAreas
                });
            }

            AddGroup(result, CommuneGroup, source.Where(IsCommuneArea), includeGroupRows, groupRowsSelectable);
            AddGroup(result, ProvinceGroup, source.Where(IsProvinceUnit), includeGroupRows, groupRowsSelectable);
            AddGroup(result, MinistryGroup, source.Where(IsMinistryUnit), includeGroupRows, groupRowsSelectable);
            AddGroup(result, ProvincialPoliceGroup, source.Where(IsProvincialPoliceUnit), includeGroupRows, groupRowsSelectable);
            AddGroup(result, ExternalPoliceGroup, source.Where(IsExternalPoliceUnit), includeGroupRows, groupRowsSelectable);

            var groupedNames = new HashSet<string>(
                result.SelectMany(item => item.IsGroup ? item.Children : Enumerable.Repeat(item, 1)).Select(item => item.DisplayName),
                StringComparer.CurrentCultureIgnoreCase);
            foreach (var area in source.Where(area => !groupedNames.Contains(area)))
            {
                result.Add(CreateLeaf(area, "Đơn vị khác"));
            }

            return result;
        }

        public static bool IsGroupFilter(string value)
        {
            return string.Equals(value, CommuneGroup, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(value, ProvinceGroup, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(value, MinistryGroup, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(value, ProvincialPoliceGroup, StringComparison.CurrentCultureIgnoreCase)
                || string.Equals(value, ExternalPoliceGroup, StringComparison.CurrentCultureIgnoreCase);
        }

        public static IEnumerable<AreaSelectionOption> Flatten(IEnumerable<AreaSelectionOption> options)
        {
            foreach (var option in options ?? Enumerable.Empty<AreaSelectionOption>())
            {
                yield return option;
                foreach (var child in option.Children)
                {
                    yield return child;
                }
            }
        }

        public static string GetDisplayName(IEnumerable<AreaSelectionOption> options, string filterValue)
        {
            return Flatten(options).FirstOrDefault(option => string.Equals(option.FilterValue, filterValue, StringComparison.CurrentCultureIgnoreCase))?.DisplayName
                ?? filterValue
                ?? string.Empty;
        }

        public static ObservableCollection<AreaSelectionOption> Filter(IEnumerable<AreaSelectionOption> options, string searchText)
        {
            var roots = (options ?? Enumerable.Empty<AreaSelectionOption>()).ToList();
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new ObservableCollection<AreaSelectionOption>(roots);
            }

            var search = NormalizeText(searchText);
            var result = new ObservableCollection<AreaSelectionOption>();
            foreach (var option in roots)
            {
                if (!option.IsGroup)
                {
                    if (IsMatch(option, search))
                    {
                        result.Add(option);
                    }

                    continue;
                }

                var groupMatches = IsMatch(option, search);
                var matchingChildren = option.Children.Where(child => groupMatches || IsMatch(child, search)).ToList();
                if (groupMatches || matchingChildren.Count > 0)
                {
                    result.Add(CloneGroup(option, matchingChildren));
                }
            }

            return result;
        }

        public static IReadOnlyList<(string AreaType, string Name)> GetStandardOrganizationAreas()
        {
            return ProvinceUnits.Select(name => (ProvinceGroup, name))
                .Concat(MinistryUnits.Select(name => (MinistryGroup, name)))
                .Concat(ProvincialPoliceUnits.Select(name => (ProvincialPoliceGroup, name)))
                .Concat(ExternalPoliceUnits.Select(name => (ExternalPoliceGroup, name)))
                .ToList();
        }

        private static void AddGroup(
            ObservableCollection<AreaSelectionOption> result,
            string groupName,
            IEnumerable<string> children,
            bool includeGroupRows,
            bool groupRowsSelectable)
        {
            var childList = children.ToList();
            if (childList.Count == 0)
            {
                return;
            }

            var group = new AreaSelectionOption
            {
                DisplayName = groupName,
                FilterValue = groupRowsSelectable ? groupName : null,
                GroupName = groupName,
                ToolTip = BuildGroupToolTip(groupName, childList),
                IsGroup = true,
                IsSelectable = groupRowsSelectable
            };

            foreach (var child in childList)
            {
                group.Children.Add(CreateLeaf(child, groupName));
            }

            if (includeGroupRows)
            {
                result.Add(group);
                return;
            }

            foreach (var child in group.Children)
            {
                result.Add(child);
            }
        }

        private static AreaSelectionOption CreateLeaf(string displayName, string groupName)
        {
            return new AreaSelectionOption
            {
                DisplayName = displayName,
                FilterValue = displayName,
                GroupName = groupName,
                ToolTip = groupName
            };
        }

        private static AreaSelectionOption CloneGroup(AreaSelectionOption source, IEnumerable<AreaSelectionOption> children)
        {
            var clone = new AreaSelectionOption
            {
                DisplayName = source.DisplayName,
                FilterValue = source.FilterValue,
                GroupName = source.GroupName,
                ToolTip = source.ToolTip,
                IsGroup = source.IsGroup,
                IsSelectable = source.IsSelectable
            };

            foreach (var child in children)
            {
                clone.Children.Add(child);
            }

            return clone;
        }

        private static bool IsMatch(AreaSelectionOption option, string normalizedSearch)
        {
            return NormalizeText(option.DisplayName).Contains(normalizedSearch)
                || NormalizeText(option.GroupName).Contains(normalizedSearch);
        }

        private static string NormalizeText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.ToLower(CultureInfo.CurrentCulture).Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return builder.ToString()
                .Replace("Ä‘", "d")
                .Replace("Ä", "d")
                .Normalize(NormalizationForm.FormC);
        }

        private static string BuildGroupToolTip(string groupName, IReadOnlyList<string> children)
        {
            var builder = new StringBuilder(groupName);
            foreach (var child in children)
            {
                builder.AppendLine();
                builder.Append("- ");
                builder.Append(child);
            }

            return builder.ToString();
        }

        private static bool IsCommuneArea(string value)
        {
            return value.StartsWith("Xã ", StringComparison.CurrentCultureIgnoreCase)
                || value.StartsWith("Phường ", StringComparison.CurrentCultureIgnoreCase)
                || value.StartsWith("Đặc khu ", StringComparison.CurrentCultureIgnoreCase);
        }

        private static bool IsProvinceUnit(string value)
        {
            return ProvinceUnits.Contains(value, StringComparer.CurrentCultureIgnoreCase);
        }

        private static bool IsMinistryUnit(string value)
        {
            return MinistryUnits.Contains(value, StringComparer.CurrentCultureIgnoreCase);
        }

        private static bool IsProvincialPoliceUnit(string value)
        {
            return ProvincialPoliceUnits.Contains(value, StringComparer.CurrentCultureIgnoreCase);
        }

        private static bool IsExternalPoliceUnit(string value)
        {
            return ExternalPoliceUnits.Contains(value, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}

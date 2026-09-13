using System.Linq;
using QuanLyHoSo.Models;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class AreaSelectionOptionsTests
    {
        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void Build_WhenInputContainsDuplicatesAndBlanks_ShouldReturnUniqueSelectableLeaves()
        {
            var options = AreaSelectionOptions.Build(new[] { "C01", "c01", " ", null, "Tùy chỉnh" }, false, false);
            var leaves = AreaSelectionOptions.Flatten(options).Where(item => !item.IsGroup).ToList();

            Assert.Equal(2, leaves.Count);
            Assert.Contains(leaves, item => item.DisplayName == "C01" && item.FilterValue == "C01");
            Assert.Contains(leaves, item => item.DisplayName == "Tùy chỉnh");
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void Filter_WhenSearchDiffersByCase_ShouldFindMatchingChildAndPreserveGroup()
        {
            var options = AreaSelectionOptions.Build(new[] { "C01", "C02", "Tùy chỉnh" }, true, true);

            var filtered = AreaSelectionOptions.Filter(options, "c01");

            var group = Assert.Single(filtered, item => item.IsGroup);
            var child = Assert.Single(group.Children);
            Assert.Equal("C01", child.DisplayName);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void Filter_WhenSearchIsWhitespace_ShouldPreserveRootItems()
        {
            var options = AreaSelectionOptions.Build(new[] { "C01", "Tùy chỉnh" }, true, true);

            var filtered = AreaSelectionOptions.Filter(options, "   ");

            Assert.Equal(options.Count, filtered.Count);
            Assert.Same(options[0], filtered[0]);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void Flatten_WhenInputIsNull_ShouldReturnEmptySequence()
        {
            Assert.Empty(AreaSelectionOptions.Flatten(null));
        }
    }
}

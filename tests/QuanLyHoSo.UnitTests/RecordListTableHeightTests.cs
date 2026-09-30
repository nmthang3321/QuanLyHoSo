using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using Moq;
using QuanLyHoSo.ApplicationServices.Abstractions;
using QuanLyHoSo.Models;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class RecordListTableHeightTests
    {
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Category", "Regression")]
        public void TableHeight_ShouldUpdateImmediatelyOnCollectionChanges()
        {
            var service = new Mock<IApplicationDataService>();
            service.Setup(x => x.GetCatalogValues(It.IsAny<string>(), It.IsAny<bool>())).Returns(new List<string>());
            service.Setup(x => x.GetAreaNames(It.IsAny<bool>())).Returns(new List<string>());
            service.Setup(x => x.GetProcessorNames(It.IsAny<bool>())).Returns(new List<string>());
            using var vm = new RecordListViewModel(service.Object, () => { }, _ => { }, (_, _) => { });
            var raised = new List<string>();
            vm.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            vm.Records.Clear();
            Assert.Contains(nameof(RecordListViewModel.TableHeight), raised);
            Assert.Equal(38 + 18, vm.TableHeight);

            var before = raised.Count;
            vm.Records.Add(NewRow());
            Assert.Contains(nameof(RecordListViewModel.TableHeight), raised.GetRange(before, raised.Count - before));
            Assert.Equal(38 + 34 + 18, vm.TableHeight);
        }

        private static RecordListRowViewModel NewRow()
        {
            ICommand command = new RelayCommand(() => { });
            return new RecordListRowViewModel(new RecentRecord(), command, command, command, command);
        }
    }
}

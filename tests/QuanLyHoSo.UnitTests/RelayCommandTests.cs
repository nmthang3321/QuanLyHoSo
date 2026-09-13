using System;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class RelayCommandTests
    {
        [Fact]
        [Trait("Category", TestCategories.Smoke)]
        [Trait("Category", TestCategories.Unit)]
        public void Execute_WhenParameterless_ShouldInvokeActionExactlyOnce()
        {
            var calls = 0;
            var command = new RelayCommand(() => calls++);

            command.Execute(null);

            Assert.Equal(1, calls);
            Assert.True(command.CanExecute(null));
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void ParameterizedCommand_ShouldPassParameterToCanExecuteAndExecute()
        {
            object executedWith = null;
            var command = new RelayCommand(value => executedWith = value, value => value is int number && number > 0);

            Assert.False(command.CanExecute(0));
            Assert.True(command.CanExecute(42));
            command.Execute(42);

            Assert.Equal(42, executedWith);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void RaiseCanExecuteChanged_ShouldNotifyEveryCall()
        {
            var notifications = 0;
            var command = new RelayCommand(() => { });
            command.CanExecuteChanged += (_, __) => notifications++;

            command.RaiseCanExecuteChanged();
            command.RaiseCanExecuteChanged();

            Assert.Equal(2, notifications);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void Constructor_WhenActionIsNull_ShouldRejectConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand((Action)null));
            Assert.Throws<ArgumentNullException>(() => new RelayCommand((Action<object>)null));
        }
    }
}

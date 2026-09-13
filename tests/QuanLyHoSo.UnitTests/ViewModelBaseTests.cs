using System.Collections.Generic;
using QuanLyHoSo.ViewModels;
using Xunit;

namespace QuanLyHoSo.UnitTests
{
    public sealed class ViewModelBaseTests
    {
        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void SetProperty_WhenValueChanges_ShouldRaiseNamedNotificationOnce()
        {
            var subject = new Subject();
            var changes = new List<string>();
            subject.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

            subject.Value = "new";
            subject.Value = "new";

            Assert.Equal(new[] { nameof(Subject.Value) }, changes);
        }

        [Fact]
        [Trait("Category", TestCategories.Unit)]
        public void Dispose_WhenCalledTwice_ShouldReleaseResourcesOnce()
        {
            var subject = new Subject();

            subject.Dispose();
            subject.Dispose();

            Assert.Equal(1, subject.DisposeCalls);
        }

        private sealed class Subject : ViewModelBase
        {
            private string _value;
            public int DisposeCalls { get; private set; }
            public string Value { get => _value; set => SetProperty(ref _value, value); }
            protected override void Dispose(bool disposing) => DisposeCalls++;
        }
    }
}

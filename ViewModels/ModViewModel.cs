using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using MWSManager.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Serilog;

namespace MWSManager.ViewModels
{
    public partial class ModViewModel : ViewModelBase
    {
        [Reactive]
        private Mod mod;

        [ObservableAsProperty]
        private string? authorsCommaSep;

        [ObservableAsProperty]
        private bool hasUpdates = false;

        public string Thumbnail => Mod.Thumbnail ?? "../Assets/DefaultModThumb.png";

        public ModViewModel(Mod mod)
		{
            Mod = mod;

            authorsCommaSepHelper = this.WhenAnyValue(x => x.Mod)
                .Select(x => x?.Authors.Count > 0 ? String.Join(", ", x.Authors) : null)
                .ToProperty(this, x => x.AuthorsCommaSep);

            // I hate this. This would be a one liner in Vue...
            hasUpdatesHelper = Mod.Updates.ToObservableChangeSet()
                .AutoRefresh(x => x.NextVersion)
                .Filter(x => x.NextVersion != null)
                .CountChanged()
                .Select(x => x.Count > 0)
                .ToProperty(this, x => x.HasUpdates);
        }
	}
}
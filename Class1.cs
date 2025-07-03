using System;
using UnityEngine;
using KSP;

namespace SilentScienceMod
{
    public class SilentScienceHelper : PartModule
    {
        private ModuleScienceExperiment experimentModule;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            experimentModule = part.FindModuleImplementing<ModuleScienceExperiment>();
        }

        [KSPAction("Silent init")]
        public void DoSilentExperiment(KSPActionParam param)
        {
            if (experimentModule == null) return;

            if (experimentModule.ExperimentIsInoperable || experimentModule.ScienceCap <= 0) return;

            var result = experimentModule.DeployExperiment();

            if (result == ModuleScienceExperiment.DeployExperimentResult.Success)
            {
                experimentModule.showResultsDialog = false;

                if (experimentModule.GetData().Count > 0)
                {
                    foreach (var data in experimentModule.GetData())
                    {
                        experimentModule.ReturnData(data);
                    }
                }
            }
        }

    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class AttachSilentScienceHelper : MonoBehaviour
    {
        public void Start()
        {
            foreach (AvailablePart part in PartLoader.LoadedPartsList)
            {
                if (part.partPrefab == null) continue;

                var hasScience = part.partPrefab.FindModuleImplementing<ModuleScienceExperiment>();

                if (hasScience != null && part.partPrefab.FindModuleImplementing<SilentScienceHelper>() == null)
                {
                    part.partPrefab.AddModule("SilentScienceMod.SilentScienceHelper");
                }
            }
        }
    }
}

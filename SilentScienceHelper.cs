using System;
using UnityEngine;
using KSP;

namespace SilentScienceMod
{
    public class SilentScienceHelper : PartModule
    {
        private ModuleScienceExperiment experiment;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            experiment = part.FindModuleImplementing<ModuleScienceExperiment>();
            if (experiment == null)
                Debug.Log("[SilentScienceMod] No ModuleScienceExperiment found on part: " + part.name);
        }

        [KSPAction("Silent Experiment Run", KSPActionGroup.None)]
        public void RunExperimentSilently(KSPActionParam param)
        {
            if (experiment == null)
            {
                //Debug.Log("[SilentScienceMod] Experiment module is null.");
                return;
            }

            var expSit = ScienceUtil.GetExperimentSituation(vessel);

            if (experiment.Inoperable || !experiment.experiment.IsAvailableWhile(expSit, vessel.mainBody))
            {
                //Debug.Log("[SilentScienceMod] Experiment not available or already used.");
                return;
            }

            if (experiment.GetData()?.Length > 0)
            {
                //Debug.Log("[SilentScienceMod] Experiment already has data, skipping.");
                return;
            }

            string biome = expSit != ExperimentSituations.SrfLanded
                ? ""
                : ScienceUtil.GetBiomedisplayName(vessel.mainBody, ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude));

            var subject = ResearchAndDevelopment.GetExperimentSubject(
                experiment.experiment,
                expSit,
                vessel.mainBody,
                biome,
                biome
            );

            if (subject == null)
            {
                //Debug.Log("[SilentScienceMod] Failed to get subject.");
                return;
            }

            float scienceAmount = experiment.experiment.baseValue * experiment.experiment.dataScale * subject.subjectValue;

            ScienceData data = new ScienceData(
                scienceAmount,
                experiment.xmitDataScalar,
                0f,
                subject.id,
                subject.title
            );

            experiment.ReturnData(data);
            experiment.Deployed = true;
            experiment.Inoperable = !experiment.rerunnable;

            //Debug.Log($"[SilentScienceMod] Science collected silently: {subject.title} | {scienceAmount} science");
            ScreenMessages.PostScreenMessage($"Silent experiment complete: {subject.title}", 3f, ScreenMessageStyle.UPPER_CENTER);

            HandleAnimation(); // <- вызов анимации
        }

        private void HandleAnimation()
        {
            string internalName = part.partInfo?.name ?? part.name;
            internalName = internalName.Replace("(Clone)", "").Trim();

            // 1. –аскрытие deployable-модулей (Magnetometer Boom)
            var deployable = part.FindModuleImplementing<ModuleDeployablePart>();
            if (deployable != null)
            {
                //Debug.Log($"[SilentScienceMod] ModuleDeployablePart found on part: {internalName} | anim: {deployable.animationName} | state: {deployable.deployState}");

                if (internalName == "Magnetometer")
                {
                    if (deployable.deployState == ModuleDeployablePart.DeployState.RETRACTED)
                    {
                        deployable.Extend();
                        //Debug.Log("[SilentScienceMod] Extend() called on Magnetometer Boom.");
                    }
                    else
                    {
                        //Debug.Log("[SilentScienceMod] Magnetometer Boom already extended or in motion.");
                    }
                }
            }

            // 2. ќбработка анимаций (Goo, Science Jr.) Ч кроме Mk2 Lander Can
            foreach (var anim in part.FindModulesImplementing<ModuleAnimateGeneric>())
            {
                //Debug.Log($"[SilentScienceMod] internalName = {internalName}");
                if (internalName == "mk2LanderCabin.v2" && anim.animationName == "Mk2Doors")
                {
                    //Debug.Log("[SilentScienceMod] Skipping Mk2Doors animation on mk2LanderCabin.v2.");
                    continue;
                }

                anim.Toggle();
                //Debug.Log($"[SilentScienceMod] Part debug info Ч part.name: {part.name}, part.partInfo.name: {part.partInfo?.name}, title: {part.partInfo?.title}");
                //Debug.Log($"[SilentScienceMod] Animation toggled: {anim.animationName}");
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
                    part.partPrefab.AddModule("SilentScienceHelper", true);
                    //Debug.Log($"[SilentScienceMod] Added SilentScienceHelper to part: {part.name}");
                }
            }
        }
    }
}

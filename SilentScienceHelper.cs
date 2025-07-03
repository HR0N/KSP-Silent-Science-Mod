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
                Debug.Log("[SilentScienceMod] Experiment module is null.");
                return;
            }

            var expSit = ScienceUtil.GetExperimentSituation(vessel);

            if (experiment.Inoperable || !experiment.experiment.IsAvailableWhile(expSit, vessel.mainBody))
            {
                Debug.Log("[SilentScienceMod] Experiment not available or already used.");
                return;
            }

            if (experiment.GetData()?.Length > 0)
            {
                Debug.Log("[SilentScienceMod] Experiment already has data, skipping.");
                return;
            }

            var biome = ScienceUtil.GetExperimentSituation(vessel) != ExperimentSituations.SrfLanded
    ? "" : ScienceUtil.GetBiomedisplayName(vessel.mainBody, ScienceUtil.GetExperimentBiome(vessel.mainBody, vessel.latitude, vessel.longitude));
            var subject = ResearchAndDevelopment.GetExperimentSubject(
                experiment.experiment,
                expSit,
                vessel.mainBody,
                biome,
                biome
            );

            if (subject == null)
            {
                Debug.Log("[SilentScienceMod] Failed to get subject.");
                return;
            }

            // Отладка для проверки subject.id
            Debug.Log($"[SilentScienceMod] subject.id type: {subject.id.GetType()} | value: {subject.id}");

            // Рассчитываем объем научных данных
            float scienceAmount = experiment.experiment.baseValue * experiment.experiment.dataScale * subject.subjectValue;

            // Отладка конструкторов ScienceData
            foreach (var ctor in typeof(ScienceData).GetConstructors())
            {
                Debug.Log($"[SilentScienceMod] ScienceData constructor: {ctor}");
            }

            // Создаем объект ScienceData (пробуем стандартный конструктор)
            ScienceData data = new ScienceData(
                scienceAmount,           // Объем научных данных
                experiment.xmitDataScalar, // Множитель передачи
                0f,                      // Лабораторный множитель (float)
                subject.id.ToString(),   // ID эксперимента (преобразуем в строку)
                subject.title            // Заголовок эксперимента
            );

            // Сохраняем данные в эксперимент
            experiment.ReturnData(data);

            // Устанавливаем статус эксперимента
            experiment.Deployed = true;
            experiment.Inoperable = !experiment.rerunnable;

            Debug.Log($"[SilentScienceMod] Science collected silently: {subject.title} | {scienceAmount} science");
            ScreenMessages.PostScreenMessage($"Silent experiment complete: {subject.title}", 3f, ScreenMessageStyle.UPPER_CENTER);
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
                    Debug.Log($"[SilentScienceMod] Added SilentScienceHelper to part: {part.name}");
                }
            }
        }
    }
}
// File:     Assets/Editor/UiToolkitSpike/Tests/UiToolkitSpikeTests.cs
// Created:  2026-11-20
// Modified: 2026-11-20 (initial T0a probe)
// Author:   —
// Spec:     UI Toolkit presentation design §4, T0a (PR #560)
// Purpose:  Establish what Unity EditMode can check on the spike's UXML and USS.

using System.Collections;

using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace TacticalDirector.UiToolkitSpike.Tests
{
    /// <summary>Checks the spike's authored structure and the style/layout available in EditMode.</summary>
    public sealed class UiToolkitSpikeTests
    {
        private const string UXML = "Assets/Editor/UiToolkitSpike/Spike.uxml";

        /// <summary>Clones the runtime markup without a panel and inspects its authored focus stops.</summary>
        [Test]
        public void CloneContainsTheOrderedKeyboardStops()
        {
            TemplateContainer tree = CloneSpike();
            var rail = tree.Q<VisualElement>("spike-rail");
            Assert.That(rail, Is.Not.Null);
            var scroll = rail.Q<ScrollView>("spike-scroll");
            Assert.That(scroll, Is.Not.Null);
            var scale = scroll.Q<Button>("cycle-scale");
            var sample = scroll.Q<Label>("long-copy");
            var submit = scroll.Q<Button>("submit-probe");
            var returnFocus = scroll.Q<Button>("return-focus");
            Assert.That(scale, Is.Not.Null);
            Assert.That(sample, Is.Not.Null);
            Assert.That(submit, Is.Not.Null);
            Assert.That(returnFocus, Is.Not.Null);

            var ordered = new VisualElement[] { scale, sample, submit, returnFocus };
            VisualElement content = scale.parent;
            int lastIndex = -1;
            foreach (VisualElement element in ordered)
            {
                Assert.That(element.parent, Is.SameAs(content), element.name);
                Assert.That(element.focusable, Is.True, element.name);
                Assert.That(element.tabIndex, Is.GreaterThanOrEqualTo(0), element.name);
                int index = content.IndexOf(element);
                Assert.That(index, Is.GreaterThan(lastIndex), element.name);
                lastIndex = index;
            }
        }

        /// <summary>Attaches the same markup to an Editor panel and checks text reflow at fixed width.</summary>
        [UnityTest]
        public IEnumerator UssDoublesTextSizeAndReflowsInAnEditorPanel()
        {
            var window = ScriptableObject.CreateInstance<SpikeLayoutWindow>();
            try
            {
                window.ShowUtility();
                var root = CloneSpike();
                root.style.width = 420;
                root.style.height = 360;
                window.rootVisualElement.Add(root);
                var rail = root.Q<VisualElement>("spike-rail");
                var sample = rail.Q<Label>("long-copy");
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (EditorApplication.timeSinceStartup < deadline &&
                    (sample.resolvedStyle.fontSize != 16f || !(sample.layout.height > 0f)))
                    yield return null;

                Assert.That(sample.resolvedStyle.fontSize, Is.EqualTo(16f));
                Assert.That(sample.layout.height, Is.GreaterThan(0f));
                float originalWidth = sample.layout.width;
                float originalHeight = sample.layout.height;

                rail.AddToClassList("spike-large-text");
                deadline = EditorApplication.timeSinceStartup + 5;
                while (EditorApplication.timeSinceStartup < deadline &&
                    (sample.resolvedStyle.fontSize != 32f || !(sample.layout.height > originalHeight)))
                    yield return null;

                Assert.That(sample.resolvedStyle.fontSize, Is.EqualTo(32f));
                Assert.That(sample.layout.width, Is.LessThanOrEqualTo(originalWidth + 1f));
                Assert.That(sample.layout.height, Is.GreaterThan(originalHeight));
            }
            finally
            {
                window.Close();
            }
        }

        private static TemplateContainer CloneSpike()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UXML);
            Assert.That(asset, Is.Not.Null, UXML);
            return asset.Instantiate();
        }

        private sealed class SpikeLayoutWindow : EditorWindow { }
    }
}

// | Version | Date       | Author | Change |
// | 0.1     | 2026-11-20 | —      | UXML structure and Editor-panel style/layout checks. |

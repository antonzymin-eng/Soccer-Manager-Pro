// File:     Assets/Editor/UiToolkitSpike/Tests/UiToolkitSpikeTests.cs
// Created:  2026-11-20
// Modified: 2026-11-20 (include the runtime style assets in the detached check)
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
    /// <summary>Checks the spike's authored structure, keyboard focus, and style/layout in EditMode.</summary>
    public sealed class UiToolkitSpikeTests
    {
        private const string SPIKE_ASSETS = "Assets/Editor/UiToolkitSpike/";

        /// <summary>Clones the runtime markup without a panel and inspects its authored focus stops.</summary>
        [Test]
        public void DetachedCloneDeclaresFocusableControlsInOrder()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<StyleSheet>(SPIKE_ASSETS + "Spike.uss"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(SPIKE_ASSETS + "Spike.tss"), Is.Not.Null);
            VisualElement[] ordered = GetKeyboardStops(CloneSpike());
            VisualElement content = ordered[0].parent;
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

        /// <summary>Dispatches Tab and Shift+Tab to the live Editor panel and observes where focus lands.</summary>
        [UnityTest]
        public IEnumerator TabMovesFocusForwardAndBackwardInAnEditorPanel()
        {
            var window = ScriptableObject.CreateInstance<SpikeLayoutWindow>();
            try
            {
                TemplateContainer root = ShowSpike(window);
                VisualElement[] ordered = GetKeyboardStops(root);
                VisualElement first = ordered[0];
                double deadline = EditorApplication.timeSinceStartup + 5;
                while (EditorApplication.timeSinceStartup < deadline &&
                    (first.panel == null || !(first.layout.height > 0f)))
                    yield return null;

                Assert.That(first.panel, Is.Not.Null, "The spike must be attached to a live Editor panel.");
                Assert.That(first.layout.height, Is.GreaterThan(0f), "The spike must have completed its first layout.");
                first.Focus();
                yield return null;
                AssertFocus(first);

                for (int i = 1; i < ordered.Length; i++)
                {
                    SendTab(ordered[i - 1], EventModifiers.None);
                    yield return null;
                    AssertFocus(ordered[i]);
                }

                for (int i = ordered.Length - 2; i >= 0; i--)
                {
                    SendTab(ordered[i + 1], EventModifiers.Shift);
                    yield return null;
                    AssertFocus(ordered[i]);
                }
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>Attaches the same markup to an Editor panel and checks text reflow at fixed width.</summary>
        [UnityTest]
        public IEnumerator UssDoublesTextSizeAndReflowsInAnEditorPanel()
        {
            var window = ScriptableObject.CreateInstance<SpikeLayoutWindow>();
            try
            {
                TemplateContainer root = ShowSpike(window);
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

        private static void SendTab(VisualElement focused, EventModifiers modifiers)
        {
            using var tab = KeyDownEvent.GetPooled('\t', KeyCode.Tab, modifiers);
            tab.target = focused;
            focused.SendEvent(tab);
        }

        private static void AssertFocus(VisualElement expected)
        {
            var focused = expected.focusController.focusedElement;
            Assert.That(focused, Is.SameAs(expected),
                $"Expected focus on {expected.name}, observed {(focused as VisualElement)?.name ?? "(none)"}.");
        }

        private static VisualElement[] GetKeyboardStops(VisualElement root)
        {
            var rail = root.Q<VisualElement>("spike-rail");
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
            return new VisualElement[] { scale, sample, submit, returnFocus };
        }

        private static TemplateContainer ShowSpike(EditorWindow window)
        {
            window.ShowUtility();
            TemplateContainer root = CloneSpike();
            root.style.width = 420;
            root.style.height = 360;
            window.rootVisualElement.Add(root);
            return root;
        }

        private static TemplateContainer CloneSpike()
        {
            string path = SPIKE_ASSETS + "Spike.uxml";
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            Assert.That(asset, Is.Not.Null, path);
            return asset.Instantiate();
        }

        private sealed class SpikeLayoutWindow : EditorWindow { }
    }
}

// | Version | Date       | Author | Change |
// | 0.1     | 2026-11-20 | —      | UXML structure and Editor-panel style/layout checks. |
// | 0.2     | 2026-11-20 | —      | Dispatch Tab and Shift+Tab in a live Editor panel and assert the focused controls. |
// | 0.3     | 2026-11-20 | —      | Check that Unity loads the USS and runtime TSS used by the probe. |

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.UI.Windows {

    using UnityEngine.UI.Windows;
    using UnityEngine.UI.Windows.Modules;
    using UnityEngine.UI.Windows.Utilities;

    [CustomEditor(typeof(WindowSystem), editorForChildClasses: true)]
    [CanEditMultipleObjects]
    public class WindowSystemEditor : Editor {

        public SerializedProperty audio;
        public SerializedProperty breadcrumbs;
        public SerializedProperty events;
        public SerializedProperty resources;
        public SerializedProperty pools;
        public SerializedProperty tweener;

        public SerializedProperty settings;
        
        private SerializedProperty emulatePlatform;
        private SerializedProperty emulateRuntimePlatform;
        
        private SerializedProperty registeredPrefabs;

        private SerializedProperty showRootOnStart;
        private SerializedProperty rootScreen;
        
        private int selectedTab {
            get {
                return EditorPrefs.GetInt("UnityEditor.UI.Windows.WindowSystem.TabIndex");
            }
            set {
                EditorPrefs.SetInt("UnityEditor.UI.Windows.WindowSystem.TabIndex", value);
            }
        }

        private Vector2 tabScrollPosition {
            get {
                return new Vector2(
                    EditorPrefs.GetFloat("UnityEditor.UI.Windows.WindowSystem.TabScrollPosition.X"),
                    EditorPrefs.GetFloat("UnityEditor.UI.Windows.WindowSystem.TabScrollPosition.Y")
                );
            }
            set {
                EditorPrefs.SetFloat("UnityEditor.UI.Windows.WindowSystem.TabScrollPosition.X", value.x);
                EditorPrefs.SetFloat("UnityEditor.UI.Windows.WindowSystem.TabScrollPosition.Y", value.y);
            }
        }

        private UnityEditorInternal.ReorderableList listModules;

        public void OnEnable() {

            this.emulatePlatform = this.serializedObject.FindProperty("emulatePlatform");
            this.emulateRuntimePlatform = this.serializedObject.FindProperty("emulateRuntimePlatform");
            this.registeredPrefabs = this.serializedObject.FindProperty("registeredPrefabs");
            this.showRootOnStart = this.serializedObject.FindProperty("showRootOnStart");
            this.rootScreen = this.serializedObject.FindProperty("rootScreen");

            this.settings = this.serializedObject.FindProperty("settings");

            { // Modules
                this.audio = this.serializedObject.FindProperty("audio");
                this.breadcrumbs = this.serializedObject.FindProperty("breadcrumbs");
                this.events = this.serializedObject.FindProperty("events");
                this.resources = this.serializedObject.FindProperty("resources");
                this.pools = this.serializedObject.FindProperty("pools");
                this.tweener = this.serializedObject.FindProperty("tweener");
            }
            
            EditorHelpers.SetFirstSibling(this.targets);

        }

        public override void OnInspectorGUI() {

            this.serializedObject.Update();
            
            GUILayoutExt.DrawComponentHeader(this.serializedObject, "UI", () => {
                
                GUILayout.Label("Window System", GUILayout.Height(36f));
                
            }, new Color(0.3f, 0.4f, 0.6f, 0.4f));
            
            GUILayout.Space(5f);
            
            var scroll = this.tabScrollPosition;
            this.selectedTab = GUILayoutExt.DrawTabs(
                this.selectedTab,
                ref scroll,
                new GUITab("Start Up", () => {

                    EditorGUILayout.PropertyField(this.emulatePlatform);
                    EditorGUILayout.PropertyField(this.emulateRuntimePlatform);
                    
                    GUILayout.Space(10f);
                    
                    EditorGUILayout.PropertyField(this.showRootOnStart);
                    EditorGUILayout.PropertyField(this.rootScreen);

                    GUILayout.Space(10f);

                    EditorGUILayout.PropertyField(this.settings);

                }),
                new GUITab("Modules", () => {

                    EditorGUILayout.PropertyField(this.breadcrumbs);
                    EditorGUILayout.PropertyField(this.events);
                    EditorGUILayout.PropertyField(this.resources);
                    EditorGUILayout.PropertyField(this.pools);
                    EditorGUILayout.PropertyField(this.tweener);
                    EditorGUILayout.PropertyField(this.audio);
                    
                }),
                new GUITab("Windows", () => {

                    var count = this.registeredPrefabs.arraySize;
                    EditorGUILayout.PropertyField(this.registeredPrefabs, new GUIContent("Registered Prefabs (" + count + ")"));

                    GUILayout.Space(10f);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Collect prefabs", GUILayout.Width(200f), GUILayout.Height(30f)) == true) {

                        var list = new List<WindowBase>();
                        var gameObjects = AssetDatabase.FindAssets("t:GameObject");
                        foreach (var guid in gameObjects) {

                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            var win = asset.GetComponent<WindowBase>();
                            if (win != null) {
                                
                                list.Add(win);
                                
                            }

                        }
                        
                        this.registeredPrefabs.ClearArray();
                        this.registeredPrefabs.arraySize = list.Count;
                        for (int i = 0; i < list.Count; ++i) {

                            this.registeredPrefabs.GetArrayElementAtIndex(i).objectReferenceValue = list[i];


                        }

                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();

                    GUILayout.Space(10f);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Make Addressables", GUILayout.Width(200f), GUILayout.Height(30f)) == true) {

                        try {

                            for (int i = 0; i < this.registeredPrefabs.arraySize; ++i) {

                                var element = this.registeredPrefabs.GetArrayElementAtIndex(i);
                                var window = element.objectReferenceValue as WindowBase;
                                if (window != null) {

                                    EditorUtility.DisplayProgressBar("Updating Addressables", window.ToString(), i / (float)this.registeredPrefabs.arraySize);

                                    var path = AssetDatabase.GetAssetPath(window);
                                    var dir = System.IO.Path.GetDirectoryName(path);
                                    dir = dir.Replace("/Screens", "/Components");
                                    var components = AssetDatabase.FindAssets("t:GameObject", new string[] { dir });
                                    foreach (var guid in components) {

                                        var p = AssetDatabase.GUIDToAssetPath(guid);
                                        var componentGo = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                                        var component = componentGo.GetComponent<WindowComponent>();
                                        if (component != null) {

                                            componentGo.SetAddressableID(p);
                                            EditorUtility.SetDirty(componentGo);

                                        }

                                    }

                                }

                                if (window is UnityEngine.UI.Windows.WindowTypes.LayoutWindowType layoutWindowType) {

                                    EditorHelpers.UpdateLayoutWindow(layoutWindowType);
                                    EditorUtility.SetDirty(layoutWindowType);

                                }

                            }

                        } catch (System.Exception ex) {
                            Debug.LogException(ex);
                        }

                        EditorUtility.ClearProgressBar();
                        
                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Make all component in all registered screens as Addressables", EditorStyles.centeredGreyMiniLabel);

                    GUILayout.Space(10f);
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Validate Resources", GUILayout.Width(200f), GUILayout.Height(30f)) == true) {

                        try {
                            
                            var gos = AssetDatabase.FindAssets("t:GameObject");
                            var i = 0;
                            foreach (var guid in gos) {

                                var path = AssetDatabase.GUIDToAssetPath(guid);
                                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                                EditorUtility.DisplayProgressBar("Validating Resources", path, i / (float)gos.Length);

                                {
                                    var allComponents = go.GetComponentsInChildren<WindowObject>(true);
                                    foreach (var component in allComponents) {

                                        EditorHelpers.FindType(component, typeof(Resource), (fieldInfo, res) => {

                                            System.Type resType = null;
                                            var resTypeAttrs = fieldInfo.GetCustomAttributes(typeof(ResourceTypeAttribute), true);
                                            if (resTypeAttrs.Length > 0) {
                                                resType = ((ResourceTypeAttribute)resTypeAttrs[0]).type;
                                            }
                                            var r = (Resource)res;
                                            WindowSystemResourcesResourcePropertyDrawer.Validate(ref r, resType);
                                            return r;

                                        });

                                    }
                                }

                                ++i;

                            }

                        } catch (System.Exception ex) {
                            Debug.LogException(ex);
                        }

                        EditorUtility.ClearProgressBar();

                    }
                    GUILayout.FlexibleSpace();
                    GUILayout.EndHorizontal();
                    GUILayout.Label("Find and Validate all Resource objects.", EditorStyles.centeredGreyMiniLabel);

                })
                );
            this.tabScrollPosition = scroll;

            /*
            GUILayout.Space(10f);

            var iter = this.serializedObject.GetIterator();
            iter.NextVisible(true);
            do {

                if (EditorHelpers.IsFieldOfType(typeof(WindowSystem), iter.propertyPath) == true) {

                    EditorGUILayout.PropertyField(iter);

                }

            } while (iter.NextVisible(false) == true);*/

            this.serializedObject.ApplyModifiedProperties();

        }

    }

}

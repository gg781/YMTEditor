﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace YMTEditor
{
    class XMLHandler
    {
        public static string CPedVariationInfo;
        private static bool bHasTexVariations = false;
        private static bool bHasDrawblVariations = false;
        private static bool bHasLowLODs = false;
        private static bool bIsSuperLOD = false;
        public static string dlcName;

        public static void LoadXML(string filePath)
        {
            XDocument xmlFile;
            if (filePath.EndsWith(".xml"))
            {
                //loading *.ymt.xml
                xmlFile = XDocument.Load(filePath);
            }
            else
            {
                //loading *.ymt
                xmlFile = XDocument.Parse(filePath);
            }

            string usedPath = filePath;
            CPedVariationInfo = xmlFile.Element("CPedVariationInfo").FirstAttribute != null
                ? xmlFile.Element("CPedVariationInfo").FirstAttribute.Value.ToString()
                : "";

            bHasTexVariations = Convert.ToBoolean(xmlFile.Elements("CPedVariationInfo").Elements("bHasTexVariations").First().FirstAttribute.Value);
            bHasDrawblVariations = Convert.ToBoolean(xmlFile.Elements("CPedVariationInfo").Elements("bHasDrawblVariations").First().FirstAttribute.Value);
            bHasLowLODs = Convert.ToBoolean(xmlFile.Elements("CPedVariationInfo").Elements("bHasLowLODs").First().FirstAttribute.Value);
            bIsSuperLOD = Convert.ToBoolean(xmlFile.Elements("CPedVariationInfo").Elements("bIsSuperLOD").First().FirstAttribute.Value);
            dlcName = xmlFile.Elements("CPedVariationInfo").Elements("dlcName").First().Value.ToString();
            MainWindow.dlcName = CPedVariationInfo;

            //generate used components
            foreach (var node in xmlFile.Descendants("availComp"))
            {
                
                var availComponents = node.Value.Split(' '); //split on space

                if(availComponents[0].Length > 3)// that means we have weird availComp from metatool, for example: "00010203FF04FF...."
                {
                    string s = availComponents[0].ToString();
                    List<String> temp = new List<String>();
                    int availlength = availComponents[0].Length;
                    int skipEvery = 2; //we have to split every 2 characters
                    for (int i = 0; i < availlength; i += skipEvery)
                    {
                        string a = s.Substring(i, skipEvery).Substring(1);
                        
                        if(a == "F")
                        {
                            a = "255";
                        }

                        temp.Add(a);
                    }

                    availComponents = temp.ToArray();

                }
                int compId = 0; //components id's
                int compIndex = 0; //order of our components in ymt
                foreach (var comp in availComponents)
                {
                    if (comp != "255")
                    {
                        string _name = Enum.GetName(typeof(YMTTypes.ComponentNumbers), compId);
                        ComponentData componentName = new ComponentData(_name, compId, compIndex, new ObservableCollection<ComponentDrawable>());
                        MainWindow.Components.Add(componentName);

                        MenuItem item = (MenuItem)MainWindow._componentsMenu.FindName(_name);
                        item.IsChecked = true;
                        compIndex++;
                    }
                    compId++;
                }
            }

            //generate used props
            int oldId = -1; //only add props once
            foreach (var node in xmlFile.Descendants("propInfo"))
            {
                foreach (var prop in xmlFile.Descendants("aPropMetaData").Elements("Item"))
                {
                    int p_anchorId = Convert.ToInt32(prop.Element("anchorId").FirstAttribute.Value);

                    if (oldId != p_anchorId)
                    {
                        string _name = Enum.GetName(typeof(YMTTypes.PropNumbers), p_anchorId);
                        PropData propName = new PropData(_name, p_anchorId, new ObservableCollection<PropDrawable>()) { propHeader = _name.ToUpper() };

                        MainWindow.Props.Add(propName);
                        MenuItem item = (MenuItem)MainWindow._propsMenu.FindName(_name);
                        item.IsChecked = true;
                        oldId = p_anchorId;
                    }
                }
            }

            //read components
            int compItemIndex = 0; //order of our components in ymt
            foreach (var node in xmlFile.Descendants("aComponentData3").Elements("Item"))
            {  
                if (compItemIndex >= MainWindow.Components.Count())
                {
                    //some ymt's have more <Item>'s in component section than defined in <availComp>, for example freemode male_heist or bikerdlc
                    //so just skip more than in availComp
                    break;
                }
                ComponentData _curComp = MainWindow.Components.ElementAt(compItemIndex); //current component (jbib/lowr/teef etc)
                int _curCompDrawablesCount = 0; //count how many component has variations (000, 001, 002, etc)
                int _curCompAvailTex = 0; // not used by game probably, total amount of textures component has (numAvailTex)
                int _curCompDrawableIndex = 0; //current drawable index in component (000, 001, 002, etc)

                foreach (var drawable_nodes in node.Descendants("aDrawblData3"))
                {
                    _curCompDrawablesCount = drawable_nodes.Elements("Item").Count();
                    _curCompAvailTex = drawable_nodes.Elements("Item").Elements("aTexData").Elements("Item").Count();
                }

                foreach (var drawable_node in node.Descendants("aDrawblData3").Elements("Item"))
                {
                    int texturesCount = drawable_node.Elements("aTexData").Elements("Item").Count();
                    int drawablePropMask = Convert.ToInt16(drawable_node.Elements("propMask").First().FirstAttribute.Value);
                    int drawableNumAlternatives = Convert.ToInt16(drawable_node.Elements("numAlternatives").First().FirstAttribute.Value);
                    bool drawableCloth = Convert.ToBoolean(drawable_node.Elements("clothData").Elements("ownsCloth").First().FirstAttribute.Value.ToString());

                    int textureIndex = 0;
                    ComponentDrawable _curDrawable = new ComponentDrawable(_curCompDrawableIndex, texturesCount, drawablePropMask, drawableNumAlternatives, drawableCloth, new ObservableCollection<ComponentTexture>(), new ObservableCollection<ComponentInfo>());
                    _curComp.compList.Add(_curDrawable);
                    foreach (var texture_node in drawable_node.Descendants("aTexData").Elements("Item"))
                    {
                        int texId = Convert.ToInt32(texture_node.Element("texId").FirstAttribute.Value);
                        _curDrawable.dTexturesTexId = Convert.ToInt32(texId);//used to set proper dropdown value
                        string texLetter = Number2String(textureIndex, false);
                        _curDrawable.drawableTextures.Add(new ComponentTexture(texLetter, texId));
                        textureIndex++;
                    }
                    _curCompDrawableIndex++;
                }
                compItemIndex++;
            }

            //load compInfo's properties
            foreach (var compInfo_node in xmlFile.Descendants("compInfos").Elements("Item"))
            {
                string comppedXml_audioID = compInfo_node.Element("pedXml_audioID").Value.ToString(); //unknown usage
                string comppedXml_audioID2 = compInfo_node.Element("pedXml_audioID2").Value.ToString(); //unknown usage

                string[] comppedXml_expressionMods = compInfo_node.Element("pedXml_expressionMods").Value.Split(' '); //probably expressionMods(?) - used for heels for example
                
                //if not 5, then it didn't split properly (probably "0000000000" value from metatool?)
                if (comppedXml_expressionMods.Length != 5)
                {
                    string s = compInfo_node.Element("pedXml_expressionMods").Value.ToString();
                    List<String> temp = new List<String>();
                    int stringlenght = s.Length;
                    int skipEvery = 2; //we have to split every 2 characters
                    for (int i = 0; i < stringlenght; i += skipEvery)
                    {
                        string a = s.Substring(i, skipEvery).Substring(1);
                        temp.Add(a);
                    }

                    comppedXml_expressionMods = temp.ToArray();
                }

                int compflags = Convert.ToInt32(compInfo_node.Element("flags").FirstAttribute.Value); //unknown usage
                string compinclusions = compInfo_node.Element("inclusions").Value.ToString(); //unknown usage
                string compexclusions = compInfo_node.Element("exclusions").Value.ToString(); //unknown usage
                string comppedXml_vfxComps = compInfo_node.Element("pedXml_vfxComps").Value.ToString(); //unknown usage - always "PV_COMP_HEAD" (?)
                int comppedXml_flags = Convert.ToInt32(compInfo_node.Element("pedXml_flags").FirstAttribute.Value); //unknown usage

                int comppedXml_compIdx = Convert.ToInt32(compInfo_node.Element("pedXml_compIdx").FirstAttribute.Value); //component id (jbib = 11, feet = 6, etc)
                int comppedXml_drawblIdx = Convert.ToInt32(compInfo_node.Element("pedXml_drawblIdx").FirstAttribute.Value); //drawable index (000, 001, 002, etc)

                string _name = Enum.GetName(typeof(YMTTypes.ComponentNumbers), comppedXml_compIdx);
                int curCompIndex = ComponentData.GetComponentIndexByID(comppedXml_compIdx);
                if (curCompIndex != -1)
                {
                    // FA1F28BF can have bigger value (in compInfo section) than there is defined drawables in component, so don't add more than exists
                    if (MainWindow.Components.ElementAt(curCompIndex).compList.Count() > comppedXml_drawblIdx)
                    {
                        //some weird ymt's can have multiple compInfo properties for one drawable, so let's add only first one
                        if(MainWindow.Components.ElementAt(curCompIndex).compList.ElementAt(comppedXml_drawblIdx).drawableInfo.Count() == 0)
                        {
                            MainWindow.Components.ElementAt(curCompIndex).compList.ElementAt(comppedXml_drawblIdx).drawableInfo.Add(new ComponentInfo(comppedXml_audioID, comppedXml_audioID2,
                                comppedXml_expressionMods, compflags, compinclusions, compexclusions, comppedXml_vfxComps, comppedXml_flags, comppedXml_compIdx, comppedXml_drawblIdx));
                        }
                        
                    }
                    
                }
            }

            //read props
            int oldAnchorId = -1; //reset index on new anchor
            int _curPropDrawableIndex = 0;
            foreach (var propinfo_node in xmlFile.Descendants("propInfo"))
            {
                foreach (var propMetaData in xmlFile.Descendants("aPropMetaData").Elements("Item"))
                {

                    string p_audioId = propMetaData.Element("audioId").Value.ToString();

                    string[] p_expressionMods = propMetaData.Element("expressionMods").Value.Split(' ');
                    //if not 5, then it didn't split properly (probably "0000000000" value from metatool?)
                    if (p_expressionMods.Length != 5)
                    {
                        string s = propMetaData.Element("expressionMods").Value.ToString();
                        List<String> temp = new List<String>();
                        int stringlenght = s.Length;
                        int skipEvery = 2; //we have to split every 2 characters
                        for (int i = 0; i < stringlenght; i += skipEvery)
                        {
                            string a = s.Substring(i, skipEvery).Substring(1);
                            temp.Add(a);
                        }

                        p_expressionMods = temp.ToArray();
                    }

                    string p_renderFlag = propMetaData.Element("renderFlags").Value.ToString();
                    int p_propFlag = Convert.ToInt32(propMetaData.Element("propFlags").FirstAttribute.Value);
                    int p_flag = Convert.ToInt32(propMetaData.Element("flags").FirstAttribute.Value);
                    int p_anchorId = Convert.ToInt32(propMetaData.Element("anchorId").FirstAttribute.Value);
                    int p_propId = Convert.ToInt32(propMetaData.Element("propId").FirstAttribute.Value);
                    int p_sticky = Convert.ToInt32(propMetaData.Element("stickyness").FirstAttribute.Value);

                    if (oldAnchorId != p_anchorId)//reset index on new anchorid
                    {
                        _curPropDrawableIndex = 0;
                    }

                    PropData _curPropData = MainWindow.Props.Where(p => p.propAnchorId == p_anchorId).First();
                    int textureCount = propMetaData.Descendants("texData").Elements("Item").Count();
                    PropDrawable _curPropDrawable = new PropDrawable(_curPropDrawableIndex, textureCount, p_audioId, p_expressionMods, new ObservableCollection<PropTexture>(), p_renderFlag, p_propFlag, p_flag, p_anchorId, p_propId, p_sticky);

                    int texturePropIndex = 0;
                    foreach (var texData in propMetaData.Descendants("texData").Elements("Item"))
                    {
                        string inclusions = texData.Element("inclusions").Value.ToString();
                        string exclusions = texData.Element("exclusions").Value.ToString();
                        int texId = Convert.ToInt32(texData.Element("texId").FirstAttribute.Value);
                        int inclusionId = Convert.ToInt32(texData.Element("inclusionId").FirstAttribute.Value);
                        int exclusionId = Convert.ToInt32(texData.Element("exclusionId").FirstAttribute.Value);
                        int distribution = Convert.ToInt32(texData.Element("distribution").FirstAttribute.Value);

                        string texLetter = Number2String(texturePropIndex, false);
                        _curPropDrawable.propTextureList.Add(new PropTexture(texLetter, inclusions, exclusions, texId, inclusionId, exclusionId, distribution));

                        texturePropIndex++;
                    }

                    _curPropData.propList.Add(_curPropDrawable);

                    _curPropDrawableIndex++;
                    oldAnchorId = p_anchorId;
                }

            }

        }

        private static XElement YMTXML_Schema(string filePath)
        {
            // TOP OF FILE || START -> CPedVariationInfo
            XElement xml = CPedVariationInfo != ""
                ? new XElement("CPedVariationInfo", new XAttribute("name", CPedVariationInfo))
                : new XElement("CPedVariationInfo");

            xml.Add(new XElement("bHasTexVariations", new XAttribute("value", bHasTexVariations)));
            xml.Add(new XElement("bHasDrawblVariations", new XAttribute("value", bHasDrawblVariations)));
            xml.Add(new XElement("bHasLowLODs", new XAttribute("value", bHasLowLODs)));
            xml.Add(new XElement("bIsSuperLOD", new XAttribute("value", bIsSuperLOD)));

            (int[] availComp, int availCompCount) = generateAvailComp();
            xml.Add(new XElement("availComp", String.Join(" ", availComp)));

            // START -> aComponentData3
            XElement components = new XElement("aComponentData3", new XAttribute("itemType", "CPVComponentData"));
            for (int i = 0; i < availCompCount; i++)
            {
                XElement compIndex = new XElement("Item");
                compIndex.Add(new XElement("numAvailTex", new XAttribute("value", countAvailTex(i))));
                XElement drawblData = new XElement("aDrawblData3", new XAttribute("itemType", "CPVDrawblData"));
                compIndex.Add(drawblData);

                for (int j = 0; j < MainWindow.Components.ElementAt(i).compList.Count(); j++)
                {
                    XElement drawblDataIndex = new XElement("Item");
                    int _propMask = MainWindow.Components.ElementAt(i).compList.ElementAt(j).drawablePropMask;
                    int _numAlternatives = MainWindow.Components.ElementAt(i).compList.ElementAt(j).drawableAlternatives;
                    drawblDataIndex.Add(new XElement("propMask", new XAttribute("value", _propMask)));
                    drawblDataIndex.Add(new XElement("numAlternatives", new XAttribute("value", _numAlternatives)));
                    XElement TexDataIndex = new XElement("aTexData", new XAttribute("itemType", "CPVTextureData"));
                    drawblDataIndex.Add(TexDataIndex);

                    foreach (var txt in MainWindow.Components.ElementAt(i).compList.ElementAt(j).drawableTextures)
                    {
                        XElement TexDataItem = new XElement("Item");
                        int _texId = txt.textureTexId;
                        TexDataItem.Add(new XElement("texId", new XAttribute("value", _texId)));
                        TexDataItem.Add(new XElement("distribution", new XAttribute("value", 255)));
                        TexDataIndex.Add(TexDataItem);
                    }

                    XElement clothDataItem = new XElement("clothData");
                    bool _clothData = MainWindow.Components.ElementAt(i).compList.ElementAt(j).drawableHasCloth;
                    clothDataItem.Add(new XElement("ownsCloth", new XAttribute("value", _clothData)));
                    drawblDataIndex.Add(clothDataItem);
                    drawblData.Add(drawblDataIndex);
                }
                if (!drawblData.IsEmpty)
                {
                    components.Add(compIndex);
                }
                
            }
            xml.Add(components);
            // END -> aComponentData3

            // START -> aSelectionSets
            xml.Add(new XElement("aSelectionSets", new XAttribute("itemType", "CPedSelectionSet"))); //never seen it used anywhere i think(?)
            // END -> aSelectionSets

            // START -> compInfos
            XElement compInfo = new XElement("compInfos", new XAttribute("itemType", "CComponentInfo"));
            foreach (var c in MainWindow.Components)
            {
                foreach (var comp in c.compList)
                {
                    XElement compInfoItem = new XElement("Item");
                    if(comp.drawableInfo.FirstOrDefault() == null) //check if there is compInfo entry when we are saving, if not, create new one default
                    {
                        compInfoItem.Add(new XElement("pedXml_audioID", "none")); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_audioID2", "none")); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_expressionMods", String.Join(" ", new string[] { "0", "0", "0", "0", "0" })));  //component expressionMods (?) - gives ability to do heels
                        compInfoItem.Add(new XElement("flags", new XAttribute("value", 0))); //not sure what it does
                        compInfoItem.Add(new XElement("inclusions", "0")); //not sure what it does
                        compInfoItem.Add(new XElement("exclusions", "0")); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_vfxComps", "PV_COMP_HEAD")); //probably everything has PV_COMP_HEAD (?)
                        compInfoItem.Add(new XElement("pedXml_flags", new XAttribute("value", 0))); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_compIdx", new XAttribute("value", c.compId))); //component id (jbib = 11, lowr = 4, etc)
                        compInfoItem.Add(new XElement("pedXml_drawblIdx", new XAttribute("value", comp.drawableIndex))); // drawable index (000, 001, 002 etc)
                    }
                    else
                    {
                        compInfoItem.Add(new XElement("pedXml_audioID", comp.drawableInfo.First().infopedXml_audioID)); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_audioID2", comp.drawableInfo.First().infopedXml_audioID2)); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_expressionMods", String.Join(" ", comp.drawableInfo.First().infopedXml_expressionMods)));  //component expressionMods (?) - gives ability to do heels
                        compInfoItem.Add(new XElement("flags", new XAttribute("value", comp.drawableInfo.First().infoFlags))); //not sure what it does
                        compInfoItem.Add(new XElement("inclusions", comp.drawableInfo.First().infoInclusions)); //not sure what it does
                        compInfoItem.Add(new XElement("exclusions", comp.drawableInfo.First().infoExclusions)); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_vfxComps", comp.drawableInfo.First().infopedXml_vfxComps)); //probably everything has PV_COMP_HEAD (?)
                        compInfoItem.Add(new XElement("pedXml_flags", new XAttribute("value", comp.drawableInfo.First().infopedXml_flags))); //not sure what it does
                        compInfoItem.Add(new XElement("pedXml_compIdx", new XAttribute("value", c.compId))); //component id (jbib = 11, lowr = 4, etc)
                        compInfoItem.Add(new XElement("pedXml_drawblIdx", new XAttribute("value", comp.drawableIndex))); // drawable index (000, 001, 002 etc)
                    }
                    
                    
                    compInfo.Add(compInfoItem);
                }
            }
            xml.Add(compInfo);
            // END -> compInfos

            // START -> propInfo
            int numAvailPropsCount = 0;
            for (int i = 0; i < MainWindow.Props.Count(); i++)
            {
                numAvailPropsCount += MainWindow.Props.ElementAt(i).propList.Count();
            }

            XElement propInfo = new XElement("propInfo");
            propInfo.Add(new XElement("numAvailProps", new XAttribute("value", numAvailPropsCount % 256)));

            XElement aPropMetaData = new XElement("aPropMetaData", new XAttribute("itemType", "CPedPropMetaData"));
            foreach (var p in MainWindow.Props)
            {
                foreach (var prop in p.propList)
                {
                    XElement aPropMetaDataItem = new XElement("Item");
                    aPropMetaDataItem.Add(new XElement("audioId", prop.propAudioId));
                    aPropMetaDataItem.Add(new XElement("expressionMods", String.Join(" ", prop.propExpressionMods)));
                    XElement texData = new XElement("texData", new XAttribute("itemType", "CPedPropTexData"));
                    foreach (var txt in prop.propTextureList)
                    {
                        XElement texDataItem = new XElement("Item");
                        texDataItem.Add(new XElement("inclusions", txt.propInclusions));
                        texDataItem.Add(new XElement("exclusions", txt.propExclusions));
                        texDataItem.Add(new XElement("texId", new XAttribute("value", txt.propTexId)));
                        texDataItem.Add(new XElement("inclusionId", new XAttribute("value", txt.propInclusionId)));
                        texDataItem.Add(new XElement("exclusionId", new XAttribute("value", txt.propExclusionId)));
                        texDataItem.Add(new XElement("distribution", new XAttribute("value", txt.propDistribution)));
                        texData.Add(texDataItem);
                    }
                    aPropMetaDataItem.Add(texData);

                    //check if there is renderFlags set, if not save it as "<renderFlags />" - earlier it was "<renderFlags></renderFlags>"
                    if(prop.propRenderFlags.Length > 0)
                    {
                        aPropMetaDataItem.Add(new XElement("renderFlags", prop.propRenderFlags));
                    }
                    else
                    {
                        aPropMetaDataItem.Add(new XElement("renderFlags"));
                    }

                    aPropMetaDataItem.Add(new XElement("propFlags", new XAttribute("value", prop.propPropFlags)));
                    aPropMetaDataItem.Add(new XElement("flags", new XAttribute("value", prop.propFlags)));
                    aPropMetaDataItem.Add(new XElement("anchorId", new XAttribute("value", prop.propAnchorId)));
                    aPropMetaDataItem.Add(new XElement("propId", new XAttribute("value", prop.propIndex))); //propId is index of a prop in the same anchorid, so we can use index instead
                    aPropMetaDataItem.Add(new XElement("stickyness", new XAttribute("value", prop.propStickyness)));
                    aPropMetaData.Add(aPropMetaDataItem);
                }
            }
            propInfo.Add(aPropMetaData);

            XElement aAnchors = new XElement("aAnchors", new XAttribute("itemType", "CAnchorProps"));
            foreach (var p in MainWindow.Props)
            {
                XElement aAnchorsItem = new XElement("Item");
                string[] props = new string[p.propList.Count()];
                for (int i = 0; i < p.propList.Count(); i++)
                {
                    props[i] = p.propList.ElementAt(i).propTextureList.Count().ToString();
                }
                aAnchorsItem.Add(new XElement("props", String.Join(" ", props)));

                string pname = p.propHeader.Substring(2);
                switch (pname.Substring(0, 1))
                {
                    case "L":
                        aAnchorsItem.Add(new XElement("anchor", "ANCHOR_LEFT_" + pname.Substring(1)));
                        break;
                    case "R":
                        aAnchorsItem.Add(new XElement("anchor", "ANCHOR_RIGHT_" + pname.Substring(1)));
                        break;
                    default:
                        aAnchorsItem.Add(new XElement("anchor", "ANCHOR_" + pname));
                        break;
                }

                aAnchors.Add(aAnchorsItem);
            }

            propInfo.Add(aAnchors);

            xml.Add(propInfo);
            // END -> propInfo


            // dlcName Field
            XElement dlcNameField = new XElement("dlcName", dlcName);
            xml.Add(dlcNameField);

            return xml;
            // END OF FILE || END -> CPedVariationInfo
        }

        public static void SaveXML(string filePath)
        {
            XElement xmlFile = YMTXML_Schema(filePath);
            xmlFile.Save(filePath);
            MessageBox.Show("Saved to: " + filePath, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static XmlDocument SaveYMT(string filePath)
        {
            XElement xmlFile = YMTXML_Schema(filePath);
            xmlFile.Save(filePath);

            //create XmlDocument from XElement (codewalker.core requires XmlDocument)
            var xmldoc = new XmlDocument();
            xmldoc.Load(xmlFile.CreateReader());

            MessageBox.Show("Saved to: " + filePath, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            return xmldoc;
        }

        private static XElement Creature_Schema(string filePath)
        {
            XElement xml = new XElement("CCreatureMetaData");

            XElement pedCompExpressions = new XElement("pedCompExpressions");
            if(MainWindow.Components.Where(c => c.compId == 6).Count() > 0)
            {
                /* lets test without first entry in components
                 * 
                //heels doesn't have that first entry but without it, fivem was sometimes crashing(?)
                XElement FirstpedCompItem = new XElement("Item");
                FirstpedCompItem.Add(new XElement("pedCompID", new XAttribute("value", String.Format("0x{0:X}", 6))));
                FirstpedCompItem.Add(new XElement("pedCompVarIndex", new XAttribute("value", String.Format("0x{0:X}", -1))));
                FirstpedCompItem.Add(new XElement("pedCompExpressionIndex", new XAttribute("value", String.Format("0x{0:X}", -1))));
                FirstpedCompItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                FirstpedCompItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 28462));
                FirstpedCompItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                FirstpedCompItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                pedCompExpressions.Add(FirstpedCompItem);

                */

                foreach (var comp in MainWindow.Components.Where(c => c.compId == 6).First().compList)
                {
                    XElement pedCompItem = new XElement("Item");
                    pedCompItem.Add(new XElement("pedCompID", new XAttribute("value", String.Format("0x{0:X}", 6))));
                    pedCompItem.Add(new XElement("pedCompVarIndex", new XAttribute("value", String.Format("0x{0:X}", comp.drawableIndex))));
                    pedCompItem.Add(new XElement("pedCompExpressionIndex", new XAttribute("value", String.Format("0x{0:X}", 4))));
                    pedCompItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                    pedCompItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 28462));
                    pedCompItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                    pedCompItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                    pedCompExpressions.Add(pedCompItem);

                }
            }
            xml.Add(pedCompExpressions);

            XElement pedPropExpressions = new XElement("pedPropExpressions");
            if(MainWindow.Props.Where(p => p.propAnchorId == 0).Count() > 0)
            {
                //all original GTA have that one first entry, without it, fivem was sometimes crashing(?)
                XElement FirstpedPropItem = new XElement("Item");
                FirstpedPropItem.Add(new XElement("pedPropID", new XAttribute("value", String.Format("0x{0:X}", 0))));
                FirstpedPropItem.Add(new XElement("pedPropVarIndex", new XAttribute("value", String.Format("0x{0:X}", -1))));
                FirstpedPropItem.Add(new XElement("pedPropExpressionIndex", new XAttribute("value", String.Format("0x{0:X}", -1))));
                FirstpedPropItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                FirstpedPropItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 13201));
                FirstpedPropItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                FirstpedPropItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                pedPropExpressions.Add(FirstpedPropItem);

                foreach (var prop in MainWindow.Props.Where(p => p.propAnchorId == 0).First().propList)
                {
                    XElement pedPropItem = new XElement("Item");
                    pedPropItem.Add(new XElement("pedPropID", new XAttribute("value", String.Format("0x{0:X}", 0))));
                    pedPropItem.Add(new XElement("pedPropVarIndex", new XAttribute("value", String.Format("0x{0:X}", prop.propIndex))));
                    pedPropItem.Add(new XElement("pedPropExpressionIndex", new XAttribute("value", String.Format("0x{0:X}", 0))));
                    pedPropItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                    pedPropItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 13201));
                    pedPropItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                    pedPropItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                    pedPropExpressions.Add(pedPropItem);
                }
            }
            xml.Add(pedPropExpressions);

            return xml;
        }

        public static XmlDocument SaveCreature(string filePath)
        {
            XElement CreatureFile = Creature_Schema(filePath);
            CreatureFile.Save(filePath);

            //create XmlDocument from XElement
            var xmldoc = new XmlDocument();
            xmldoc.Load(CreatureFile.CreateReader());

            MessageBox.Show("Saved to: " + filePath, "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            return xmldoc;
        }

        public static String Number2String(int number, bool isCaps)
        {
            Char c = (Char)((isCaps ? 65 : 97) + (number));
            return c.ToString();
        }

        private static (int[], int) generateAvailComp()
        {
            int[] genAvailComp = { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255 };
            int compCount = 0;
            foreach (var comp in MainWindow.Components)
            {
                genAvailComp[comp.compId] = compCount;
                compCount++;
            }

            return (genAvailComp, compCount);
        }

        private static int countAvailTex(int componentIndex)
        {
            int _textures = 0;
            foreach (var comp in MainWindow.Components.ElementAt(componentIndex).compList)
            {
                _textures = _textures + comp.drawableTextureCount;
            }

            return _textures % 256; //numAvailTex is byte -> (gunrunning female, uppr has 720 .ytd's and in .ymt its 208)
        }
    }
}
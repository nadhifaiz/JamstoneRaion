using System.Collections.Generic;
using UnityEngine;

// Data netral untuk panel rincian
public class ItemDetailData
{
    public Sprite icon;
    public string title;
    public string typeLabel;
    public List<string> infoLines = new List<string>();
    public string description;
    public List<MaterialType> tags = new List<MaterialType>();
}
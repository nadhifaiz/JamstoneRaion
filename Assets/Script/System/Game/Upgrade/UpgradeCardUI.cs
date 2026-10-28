using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Satu kartu upgrade di panel Upgrade. Pasang di root prefab kartu.
public class UpgradeCardUI : MonoBehaviour
{
    private const string ColorOk = "#6FCF6F";
    private const string ColorLack = "#E05A5A";

    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI effectText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI requirementsText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject maxedBadge;   // opsional: label "MAKS"

    public void Bind(UpgradeSO upgrade, UpgradeManager manager, Action<UpgradeSO> onBuy)
    {
        iconImage.sprite = upgrade.icon;
        iconImage.enabled = upgrade.icon != null;
        nameText.text = upgrade.displayName;
        levelText.text = $"Lv {manager.GetLevel(upgrade)}/{upgrade.MaxLevel}";

        UpgradeLevel next = manager.GetNextLevel(upgrade);
        bool maxed = next == null;

        if (maxedBadge != null) maxedBadge.SetActive(maxed);
        buyButton.gameObject.SetActive(!maxed);

        if (maxed)
        {
            effectText.text = "Level maksimum";
            costText.text = "";
            requirementsText.text = "";
            return;
        }

        effectText.text = next.effectLabel;

        bool canAfford = manager.CanAfford(next);
        costText.text = $"<color={(canAfford ? ColorOk : ColorLack)}>{next.amberCost} Amber</color>";

        requirementsText.text = BuildRequirementsText(manager, next);

        buyButton.interactable = canAfford && manager.MeetsRequirements(next);
        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy(upgrade));
    }

    private static string BuildRequirementsText(UpgradeManager manager, UpgradeLevel level)
    {
        var sb = new StringBuilder();
        foreach (UpgradeRequirement req in level.requirements)
        {
            int owned = manager.GetOwnedCount(req);
            string color = owned >= req.count ? ColorOk : ColorLack;

            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"<color={color}>{req.DisplayName}  {Mathf.Min(owned, req.count)}/{req.count}</color>");
        }
        return sb.ToString();
    }
}
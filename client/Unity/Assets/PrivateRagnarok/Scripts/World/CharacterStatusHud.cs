using UnityEngine;
using UnityEngine.UI;

namespace PrivateRagnarok.Client
{
    /// <summary>
    /// Compact top-right RPG character card. Values are local demo defaults until
    /// authoritative character-stat endpoints are connected.
    /// </summary>
    public sealed class CharacterStatusHud : MonoBehaviour
    {
        private Text nameText, classText, levelText, hpText, spText, expText, zenyText, mapText;
        private Image hpFill, spFill, expFill;
        private Transform trackedPlayer;
        private float maxHp = 100f, hp = 100f, maxSp = 50f, sp = 50f;
        private float exp, expToNext = 100f;
        private int level = 1;
        private string characterName = "Adventurer";
        private string jobName = "Novice";
        private int zeny;

        public void Initialize(Transform player, string displayName)
        {
            trackedPlayer = player;
            if (!string.IsNullOrWhiteSpace(displayName)) characterName = displayName;
            Build();
            Refresh();
        }

        public void SetCharacter(string displayName, string job, int characterLevel, int currency)
        {
            if (!string.IsNullOrWhiteSpace(displayName)) characterName = displayName;
            if (!string.IsNullOrWhiteSpace(job)) jobName = job;
            level = Mathf.Max(1, characterLevel);
            zeny = Mathf.Max(0, currency);
            Refresh();
        }

        public void SetVitals(float currentHp, float maximumHp, float currentSp, float maximumSp)
        {
            maxHp = Mathf.Max(1f, maximumHp);
            maxSp = Mathf.Max(1f, maximumSp);
            hp = Mathf.Clamp(currentHp, 0f, maxHp);
            sp = Mathf.Clamp(currentSp, 0f, maxSp);
            Refresh();
        }

        public void SetExperience(float currentExp, float requiredExp)
        {
            expToNext = Mathf.Max(1f, requiredExp);
            exp = Mathf.Clamp(currentExp, 0f, expToNext);
            Refresh();
        }

        private void Build()
        {
            var canvasObject = new GameObject("Character HUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var panel = Rect("Character Status Panel", canvasObject.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f),
                new Vector2(440f, 202f));
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.035f, 0.045f, 0.065f, 0.88f);

            var accent = Rect("Gold Accent", panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -3f), new Vector2(-4f, 5f));
            accent.gameObject.AddComponent<Image>().color = new Color(0.82f, 0.62f, 0.25f, 1f);

            var avatar = Rect("Avatar Emblem", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(43f, -43f), new Vector2(58f, 58f));
            avatar.gameObject.AddComponent<Image>().color = new Color(0.17f, 0.24f, 0.34f, 1f);
            var avatarText = MakeText("A", avatar, new Vector2(0f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, 30, new Color(0.96f, 0.78f, 0.38f));
            avatarText.alignment = TextAnchor.MiddleCenter;

            nameText = MakeText(characterName, panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(84f, -27f), new Vector2(-104f, 34f), 27, Color.white);
            nameText.fontStyle = FontStyle.Bold;
            classText = MakeText("NOVICE  •  ONLINE", panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(84f, -58f), new Vector2(-104f, 25f), 16, new Color(0.77f, 0.79f, 0.83f));
            levelText = MakeText("BASE LV. 1", panel, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-18f, -27f), new Vector2(116f, 28f), 17, new Color(0.96f, 0.78f, 0.38f));
            levelText.alignment = TextAnchor.MiddleRight;

            hpText = MakeText("HP 100 / 100", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -91f), new Vector2(116f, 22f), 15, Color.white);
            hpFill = MakeBar(panel, "HP Bar", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(150f, -91f), new Vector2(270f, 13f), new Color(0.78f, 0.18f, 0.19f));

            spText = MakeText("SP 50 / 50", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -122f), new Vector2(116f, 22f), 15, Color.white);
            spFill = MakeBar(panel, "SP Bar", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(150f, -122f), new Vector2(270f, 13f), new Color(0.18f, 0.48f, 0.86f));

            expText = MakeText("EXP 0%", panel, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(18f, -153f), new Vector2(116f, 22f), 14, new Color(0.95f, 0.82f, 0.48f));
            expFill = MakeBar(panel, "EXP Bar", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(150f, -153f), new Vector2(270f, 10f), new Color(0.88f, 0.65f, 0.18f));

            zenyText = MakeText("0 Zeny", panel, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(18f, 15f), new Vector2(160f, 24f), 15, new Color(0.96f, 0.78f, 0.38f));
            mapText = MakeText("TOWN SQUARE", panel, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-18f, 15f), new Vector2(230f, 24f), 14, new Color(0.75f, 0.81f, 0.86f));
            mapText.alignment = TextAnchor.MiddleRight;
        }

        private void Update()
        {
            if (trackedPlayer != null)
            {
                var p = trackedPlayer.position;
                mapText.text = "FIELD  " + p.x.ToString("0") + ", " + p.z.ToString("0");
            }
        }

        private void Refresh()
        {
            if (nameText == null) return;
            nameText.text = characterName;
            classText.text = jobName.ToUpperInvariant() + "  •  ONLINE";
            levelText.text = "BASE LV. " + level;
            hpText.text = "HP " + Mathf.CeilToInt(hp) + " / " + Mathf.CeilToInt(maxHp);
            spText.text = "SP " + Mathf.CeilToInt(sp) + " / " + Mathf.CeilToInt(maxSp);
            expText.text = "EXP " + Mathf.FloorToInt(exp / expToNext * 100f) + "%";
            zenyText.text = zeny.ToString("N0") + " Zeny";
            hpFill.fillAmount = hp / maxHp;
            spFill.fillAmount = sp / maxSp;
            expFill.fillAmount = exp / expToNext;
        }

        private static Image MakeBar(Transform parent, string name, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(name + " Track", parent, min, max, position, size);
            rect.gameObject.AddComponent<Image>().color = new Color(0.12f, 0.14f, 0.17f, 1f);
            var fillRect = Rect(name + " Fill", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            return fill;
        }

        private static Text MakeText(string value, Transform parent, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var rect = Rect(value, parent, min, max, position, size);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(min.x == 1f ? 1f : 0f, min.y == 1f ? 1f : 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
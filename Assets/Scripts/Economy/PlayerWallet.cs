using System;
using UnityEngine;

namespace SeekingForJade.Economy
{
    public class PlayerWallet : MonoBehaviour
    {
        public static PlayerWallet Instance { get; private set; }

        [Header("Starting Funds")]
        [SerializeField] private int cash = 150;

        [Header("Notifications")]
        [SerializeField] private string lastTransactionMessage = "";
        [SerializeField] private float messageTimer = 0f;

        public int Cash => cash;

        public event Action<int> OnCashChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                {
                    lastTransactionMessage = "";
                }
            }
        }

        public void AddMoney(int amount, string reason = "")
        {
            if (amount <= 0) return;
            cash += amount;
            lastTransactionMessage = $"+${amount:N0} ({reason})";
            messageTimer = 3.5f;
            OnCashChanged?.Invoke(cash);
        }

        public bool TrySpendMoney(int amount, string item = "")
        {
            if (amount <= 0 || cash < amount) return false;
            cash -= amount;
            lastTransactionMessage = $"-${amount:N0} ({item})";
            messageTimer = 3.5f;
            OnCashChanged?.Invoke(cash);
            return true;
        }

        private void OnGUI()
        {
            // Top-right wallet display
            float boxWidth = 220;
            float boxHeight = 45;
            float x = Screen.width - boxWidth - 25;
            float y = 25;

            GUIStyle walletStyle = new GUIStyle(GUI.skin.box);
            walletStyle.fontSize = 20;
            walletStyle.fontStyle = FontStyle.Bold;
            walletStyle.normal.textColor = new Color(0.2f, 0.95f, 0.4f);
            walletStyle.alignment = TextAnchor.MiddleCenter;

            GUI.Box(new Rect(x, y, boxWidth, boxHeight), $"Cash: ${cash:N0}", walletStyle);

            // Floating transaction notice
            if (!string.IsNullOrEmpty(lastTransactionMessage))
            {
                GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
                msgStyle.fontSize = 16;
                msgStyle.fontStyle = FontStyle.Bold;
                msgStyle.normal.textColor = lastTransactionMessage.StartsWith("+") 
                    ? new Color(0.3f, 1.0f, 0.4f) 
                    : new Color(1.0f, 0.4f, 0.4f);
                msgStyle.alignment = TextAnchor.MiddleRight;

                GUI.Label(new Rect(x - 200, y + 8, 190, 30), lastTransactionMessage, msgStyle);
            }
        }
    }
}

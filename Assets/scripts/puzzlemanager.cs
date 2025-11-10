using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PuzzleManager : MonoBehaviour
{
    [Header("Highlight Settings")]
    public Material highlightMaterial;
    private Dictionary<Renderer, Material> originalMaterials = new Dictionary<Renderer, Material>();

    [Header("Setup")]
    public Transform puzzlePiecesParent;
    public EyeController[] eyes;
    public XRSocketInteractor[] sockets;

    [Header("Socket Material Swap")]
    public Material socketReplacementMaterial; // assign finmat in inspector
    private Dictionary<Renderer, Material> socketOriginalMaterials = new Dictionary<Renderer, Material>();
    private bool socketMaterialsChanged = false;

    [Header("Reward Settings")]
    public AudioClip rewardSound;
    public int coinAmount = 1;
    public Text coinText;

    [Header("Success Message UI")]
    public Text messageText;

    [Header("Timer UI")]
    public Text timerText;
    private float elapsedTime = 0f;
    private bool timerRunning = false;

    [Header("Instructions UI")]
    public GameObject instructionsPanel;
    public TMP_InputField playerNameInput;
    public Button startButton;

    [Header("Leaderboard UI")]
    public GameObject leaderboardPanel;
    public LeaderboardManager leaderboardManager;

    private List<XRGrabInteractable> pieces = new List<XRGrabInteractable>();
    private XRGrabInteractable currentPiece;
    private AudioSource audioSource;
    private int coins = 0;
    private int totalCoins = 0;
    private int currentSceneIndex;
    private XRMovementLock movementLock;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        movementLock = FindObjectOfType<XRMovementLock>();

        if (movementLock != null)
            movementLock.LockMovement(true);

        if (messageText != null)
        {
            messageText.text = "";
            messageText.gameObject.SetActive(false);
        }

        // Restore saved name
        if (playerNameInput != null)
        {
            string savedName = PlayerPrefs.GetString("PlayerName", "");
            playerNameInput.text = savedName;
        }

        foreach (Transform child in puzzlePiecesParent)
        {
            XRGrabInteractable grab = child.GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                SetPieceActive(grab, false);
                pieces.Add(grab);
            }
        }

        foreach (var socket in sockets)
        {
            socket.selectEntered.AddListener(OnPiecePlaced);

            Renderer rend = socket.GetComponent<Renderer>();
            if (rend == null) rend = socket.GetComponentInChildren<Renderer>();
            if (rend != null && !socketOriginalMaterials.ContainsKey(rend))
            {
                socketOriginalMaterials[rend] = rend.sharedMaterial;
                rend.enabled = true;
            }
        }

        UpdateCoinUI();
        SelectNextPiece();

        // Show instructions only in scene 0
        if (currentSceneIndex == 0 && instructionsPanel != null)
        {
            instructionsPanel.SetActive(true);
            timerRunning = false;
        }
        else
        {
            if (instructionsPanel != null)
                instructionsPanel.SetActive(false);

            StartGame();
        }

        if (startButton != null)
            startButton.onClick.AddListener(StartGame);
    }

    void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    public void StartGame()
    {
        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        // Save name persistently
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
            PlayerPrefs.SetString("PlayerName", playerNameInput.text);

        if (movementLock != null)
            movementLock.LockMovement(false);

        elapsedTime = 0f;
        timerRunning = true;

        if (!socketMaterialsChanged)
            StartCoroutine(ChangeSocketMaterialsAfterDelay(3f));
    }

    System.Collections.IEnumerator ChangeSocketMaterialsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        foreach (var kvp in socketOriginalMaterials)
        {
            Renderer rend = kvp.Key;
            if (rend == null) continue;

            if (socketReplacementMaterial != null)
                rend.material = socketReplacementMaterial;
            else
            {
                Material m = rend.material;
                if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = 0f;
                    m.color = c;
                }
            }
        }
        socketMaterialsChanged = true;
    }

    void UpdateTimerUI()
    {
        if (timerText != null)
            timerText.text = FormatTime(elapsedTime);
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time * 1000f) % 1000f);
        return string.Format("{0:00}:{1:00}:{2:000}", minutes, seconds, milliseconds);
    }

    void HighlightPiece()
    {
        if (currentPiece == null) return;
        Renderer rend = currentPiece.GetComponent<Renderer>();
        if (rend == null) return;
        if (!originalMaterials.ContainsKey(rend))
            originalMaterials[rend] = rend.sharedMaterial;
        rend.material = highlightMaterial;
        Invoke(nameof(RemoveHighlight), 2f);
    }

    void RemoveHighlight()
    {
        if (currentPiece == null) return;
        Renderer rend = currentPiece.GetComponent<Renderer>();
        if (rend != null && originalMaterials.ContainsKey(rend))
            rend.material = originalMaterials[rend];
    }

    void SelectNextPiece()
    {
        if (pieces.Count == 0)
        {
            Debug.Log("All pieces placed!");
            return;
        }

        int index = Random.Range(0, pieces.Count);
        currentPiece = pieces[index];

        foreach (var piece in pieces)
            SetPieceActive(piece, piece == currentPiece);

        CancelInvoke(nameof(HighlightPiece));
        CancelInvoke(nameof(RemoveHighlight));
        HighlightPiece();
        InvokeRepeating(nameof(HighlightPiece), 5f, 10f);

        foreach (var eye in eyes)
            eye.SetTarget(currentPiece.transform);
    }

    void OnPiecePlaced(SelectEnterEventArgs args)
    {
        XRGrabInteractable placedPiece = args.interactableObject as XRGrabInteractable;
        if (placedPiece == null) return;

        if (placedPiece == currentPiece)
        {
            CancelInvoke(nameof(HighlightPiece));
            CancelInvoke(nameof(RemoveHighlight));
            RemoveHighlight();

            SetPieceActive(placedPiece, false);
            pieces.Remove(currentPiece);

            foreach (var eye in eyes)
                eye.SetTarget(null);

            if (rewardSound != null)
                audioSource.PlayOneShot(rewardSound);

            coins += coinAmount;
            totalCoins += coinAmount;
            PlayerPrefs.SetInt("TotalCoins", totalCoins);

            UpdateCoinUI();

            if (coins >= 9)
            {
                timerRunning = false;

                string playerName = PlayerPrefs.GetString("PlayerName", "Player");
                float previousTime = PlayerPrefs.GetFloat("PrevTime", 0f);
                float totalTime = previousTime + elapsedTime;
                PlayerPrefs.SetFloat("PrevTime", totalTime);

                if (currentSceneIndex == 0)
                    Invoke(nameof(GoToNextScene), 2f);
                else
                {
                    leaderboardManager.SaveEntry(playerName, totalTime);
                    leaderboardPanel.SetActive(true);
                    leaderboardManager.DisplayLeaderboard();
                    PlayerPrefs.DeleteKey("PrevTime");
                }
            }
            else
                Invoke(nameof(SelectNextPiece), 1f);
        }
    }

    void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = "Coins: " + coins;
    }

    void SetPieceActive(XRGrabInteractable piece, bool active)
    {
        piece.enabled = active;
        Collider col = piece.GetComponent<Collider>();
        if (col != null)
            col.enabled = active;
        Rigidbody rb = piece.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = !active;
            rb.constraints = active ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
        }
    }

    void GoToNextScene()
    {
        SceneManager.LoadScene("war"); // your next scene
    }
}

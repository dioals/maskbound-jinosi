// This code is part of the Fungus library (https://github.com/snozbot/fungus)
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Fungus.Lua;
using Fungus.DentedPixel;

namespace Fungus
{
    /// <summary>
    /// Display story text in a visual novel style dialog box.
    /// </summary>
    public class SayDialog : MonoBehaviour
    {
        [Tooltip("Duration to fade dialogue in/out")]
        [SerializeField] protected float fadeDuration = 0.25f;

        [Tooltip("The continue button UI object")]
        [SerializeField] protected Button continueButton;

        [Tooltip("The canvas UI object")]
        [SerializeField] protected Canvas dialogCanvas;

        [Tooltip("The name text UI object")]
        [SerializeField] protected Text nameText;
        [Tooltip("TextAdapter will search for appropriate output on this GameObject if nameText is null")]
        [SerializeField] protected GameObject nameTextGO;
        protected TextAdapter nameTextAdapter = new TextAdapter();
        public virtual string NameText
        {
            get
            {
                return nameTextAdapter.Text;
            }
            set
            {
                nameTextAdapter.Text = value;
            }
        }

        [Tooltip("The character subtitle text UI object (e.g. title/description under the name)")]
        [SerializeField] protected Text subtitleText;
        protected TextAdapter subtitleTextAdapter = new TextAdapter();
        public virtual string SubtitleText
        {
            get
            {
                return subtitleTextAdapter.Text;
            }
            set
            {
                subtitleTextAdapter.Text = value;
            }
        }

        [Tooltip("The story text UI object")]
        [SerializeField] protected Text storyText;
        [Tooltip("TextAdapter will search for appropriate output on this GameObject if storyText is null")]
        [SerializeField] protected GameObject storyTextGO;
        protected TextAdapter storyTextAdapter = new TextAdapter();
        public virtual string StoryText
        {
            get
            {
                return storyTextAdapter.Text;
            }
            set
            {
                storyTextAdapter.Text = value;
            }
        }
        public virtual RectTransform StoryTextRectTrans
        {
            get
            {
                return storyText != null ? storyText.rectTransform : storyTextGO.GetComponent<RectTransform>();
            }
        }

        [Tooltip("The character UI object")]
        [SerializeField] protected Image characterImage;
        public virtual Image CharacterImage { get { return characterImage; } }

        [Tooltip("The dialog box background image. Can be swapped per character via Character.DialogPanel.")]
        [SerializeField] protected Image panelImage;
        protected Sprite defaultPanelSprite;

        [Tooltip("Dialog box background image used when the speaking character is on the right side. Must be a horizontally flipped version of the default panel.")]
        [SerializeField] protected Image panelImageRight;
        protected Sprite defaultPanelSpriteRight;
    
        [Tooltip("Adjust width of story text when Character Image is displayed (to avoid overlapping)")]
        [SerializeField] protected bool fitTextWithImage = true;

        [Tooltip("Close any other open Say Dialogs when this one is active")]
        [SerializeField] protected bool closeOtherDialogs;

        protected float startStoryTextWidth; 
        protected float startStoryTextInset;

        protected WriterAudio writerAudio;
        protected Writer writer;
        protected CanvasGroup canvasGroup;

        protected bool fadeWhenDone = true;
        protected float targetAlpha = 0f;
        protected float fadeCoolDownTimer = 0f;

        protected Sprite currentCharacterImage;

        // Most recent speaking character
        protected static Character speakingCharacter;

        protected StringSubstituter stringSubstituter = new StringSubstituter();

		// Cache active Say Dialogs to avoid expensive scene search
		protected static List<SayDialog> activeSayDialogs = new List<SayDialog>();

		// Cached authored layout of UI elements, used to mirror them to the right side
		protected Dictionary<RectTransform, RectLayoutCache> mirroredLayoutCache = new Dictionary<RectTransform, RectLayoutCache>();

		protected struct RectLayoutCache
		{
			public Vector2 anchorMin;
			public Vector2 anchorMax;
			public Vector2 anchoredPosition;

			public RectLayoutCache(Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
			{
				this.anchorMin = anchorMin;
				this.anchorMax = anchorMax;
				this.anchoredPosition = anchoredPosition;
			}
		}

		protected virtual void Awake()
		{
			if (!activeSayDialogs.Contains(this))
			{
				activeSayDialogs.Add(this);
			}

            nameTextAdapter.InitFromGameObject(nameText != null ? nameText.gameObject : nameTextGO);
            subtitleTextAdapter.InitFromGameObject(subtitleText != null ? subtitleText.gameObject : null);
            storyTextAdapter.InitFromGameObject(storyText != null ? storyText.gameObject : storyTextGO);
            if (panelImage != null)
            {
                defaultPanelSprite = panelImage.sprite;
            }
            if (panelImageRight != null)
            {
                defaultPanelSpriteRight = panelImageRight.sprite;
            }
        }

		protected virtual void OnDestroy()
		{
			activeSayDialogs.Remove(this);
		}
			
		protected virtual Writer GetWriter()
        {
            if (writer != null)
            {
                return writer;
            }

            writer = GetComponent<Writer>();
            if (writer == null)
            {
                writer = gameObject.AddComponent<Writer>();
            }

            return writer;
        }

        protected virtual CanvasGroup GetCanvasGroup()
        {
            if (canvasGroup != null)
            {
                return canvasGroup;
            }
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            return canvasGroup;
        }

        protected virtual WriterAudio GetWriterAudio()
        {
            if (writerAudio != null)
            {
                return writerAudio;
            }
            
            writerAudio = GetComponent<WriterAudio>();
            if (writerAudio == null)
            {
                writerAudio = gameObject.AddComponent<WriterAudio>();
            }
            
            return writerAudio;
        }

        protected virtual void Start()
        {
            // Dialog always starts invisible, will be faded in when writing starts
            GetCanvasGroup().alpha = 0f;

            // Add a raycaster if none already exists so we can handle dialog input
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();    
            }

            // It's possible that SetCharacterImage() has already been called from the
            // Start method of another component, so check that no image has been set yet.
            // Same for nameText.

            if (NameText == "")
            {
                SetCharacterName("", Color.white);
            }
            if (currentCharacterImage == null)
            {                
                // Character image is hidden by default.
                SetCharacterImage(null);
            }
        }

        protected virtual void LateUpdate()
        {
            UpdateAlpha();

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive( GetWriter().IsWaitingForInput );
            }
        }

        protected virtual void UpdateAlpha()
        {
            if (GetWriter().IsWriting)
            {
                targetAlpha = 1f;
                fadeCoolDownTimer = 0.1f;
            }
            else if (fadeWhenDone && Mathf.Approximately(fadeCoolDownTimer, 0f))
            {
                targetAlpha = 0f;
            }
            else
            {
                // Add a short delay before we start fading in case there's another Say command in the next frame or two.
                // This avoids a noticeable flicker between consecutive Say commands.
                fadeCoolDownTimer = Mathf.Max(0f, fadeCoolDownTimer - Time.deltaTime);
            }

            CanvasGroup canvasGroup = GetCanvasGroup();
            if (fadeDuration <= 0f)
            {
                canvasGroup.alpha = targetAlpha;
            }
            else
            {
                float delta = (1f / fadeDuration) * Time.deltaTime;
                float alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, delta);
                canvasGroup.alpha = alpha;

                if (alpha <= 0f)
                {                   
                    // Deactivate dialog object once invisible
                    gameObject.SetActive(false);
                }
            }
        }

        protected virtual void ClearStoryText()
        {
            StoryText = "";
        }

        #region Public members

		public Character SpeakingCharacter { get { return speakingCharacter; } }

        /// <summary>
        /// Currently active Say Dialog used to display Say text
        /// </summary>
        public static SayDialog ActiveSayDialog { get; set; }

        /// <summary>
        /// Returns a SayDialog by searching for one in the scene or creating one if none exists.
        /// </summary>
        public static SayDialog GetSayDialog()
        {
            if (ActiveSayDialog == null)
            {
				SayDialog sd = null;

				// Use first active Say Dialog in the scene (if any)
				if (activeSayDialogs.Count > 0)
				{
					sd = activeSayDialogs[0];
				}

                if (sd != null)
                {
                    ActiveSayDialog = sd;
                }

                if (ActiveSayDialog == null)
                {
                    // Auto spawn a say dialog object from the prefab
                    GameObject prefab = Resources.Load<GameObject>("Prefabs/SayDialog");
                    if (prefab != null)
                    {
                        GameObject go = Instantiate(prefab) as GameObject;
                        go.SetActive(false);
                        go.name = "SayDialog";
                        ActiveSayDialog = go.GetComponent<SayDialog>();
                    }
                }
            }

            return ActiveSayDialog;
        }

        /// <summary>
        /// Stops all active portrait tweens.
        /// </summary>
        public static void StopPortraitTweens()
        {
            // Stop all tweening portraits
            var activeCharacters = Character.ActiveCharacters;
            for (int i = 0; i < activeCharacters.Count; i++)
            {
                var c = activeCharacters[i];
                if (c.State.portraitImage != null)
                {
                    if (LeanTween.isTweening(c.State.portraitImage.gameObject))
                    {
                        LeanTween.cancel(c.State.portraitImage.gameObject, true);
                        PortraitController.SetRectTransform(c.State.portraitImage.rectTransform, c.State.position);
                        if (c.State.dimmed == true)
                        {
                            c.State.portraitImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
                        }
                        else
                        {
                            c.State.portraitImage.color = Color.white;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the active state of the Say Dialog gameobject.
        /// </summary>
        public virtual void SetActive(bool state)
        {
            gameObject.SetActive(state);
        }

        /// <summary>
        /// Sets the active speaking character.
        /// </summary>
        /// <param name="character">The active speaking character.</param>
        public virtual void SetCharacter(Character character)
        {
            if (character == null)
            {
                if (characterImage != null)
                {
                    characterImage.gameObject.SetActive(false);
                }
                if (NameText != null)
                {
                    NameText = "";
                }
                if (subtitleText != null)
                {
                    SubtitleText = "";
                }
                SetPanelImage(null);
                speakingCharacter = null;
            }
            else
            {
                var prevSpeakingCharacter = speakingCharacter;
                speakingCharacter = character;

                // Dim portraits of non-speaking characters
                var activeStages = Stage.ActiveStages;
                for (int i = 0; i < activeStages.Count; i++)
                {
                    var stage = activeStages[i];
                    if (stage.DimPortraits)
                    {
                        var charactersOnStage = stage.CharactersOnStage;
                        for (int j = 0; j < charactersOnStage.Count; j++)
                        {
                            var c = charactersOnStage[j];
                            if (prevSpeakingCharacter != speakingCharacter)
                            {
                                if (c != null && !c.Equals(speakingCharacter))
                                {
                                    stage.SetDimmed(c, true);
                                }
                                else
                                {
                                    stage.SetDimmed(c, false);
                                }
                            }
                        }
                    }
                }

                string characterName = character.NameText;

                if (characterName == "")
                {
                    // Use game object name as default
                    characterName = character.GetObjectName();
                }
                    
                SetCharacterName(characterName, character.NameColor);

                if (subtitleText != null)
                {
                    SubtitleText = character.GetDescription();
                }

                SetDialogSide(character.DialogSide);
                SetPanelImage(character.DialogPanel);
            }
        }

        /// <summary>
        /// Sets the character image to display on the Say Dialog.
        /// </summary>
        public virtual void SetCharacterImage(Sprite image)
        {
            if (characterImage == null)
            {
                return;
            }

            if (image != null)
            {
                characterImage.overrideSprite = image;
                characterImage.gameObject.SetActive(true);
                currentCharacterImage = image;
            }
            else
            {
                characterImage.gameObject.SetActive(false);

                if (startStoryTextWidth != 0)
                {
                    StoryTextRectTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 
                        startStoryTextInset, 
                        startStoryTextWidth);
                }
            }

            // Adjust story text box to not overlap image rect
            if (fitTextWithImage && 
                StoryText != null &&
                characterImage.gameObject.activeSelf)
            {
                if (Mathf.Approximately(startStoryTextWidth, 0f))
                {
                    startStoryTextWidth = StoryTextRectTrans.rect.width;
                    startStoryTextInset = StoryTextRectTrans.offsetMin.x; 
                }

                // Clamp story text to left or right depending on relative position of the character image
                if (StoryTextRectTrans.position.x < characterImage.rectTransform.position.x)
                {
                    StoryTextRectTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, 
                        startStoryTextInset, 
                        startStoryTextWidth - characterImage.rectTransform.rect.width);
                }
                else
                {
                    StoryTextRectTrans.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 
                        startStoryTextInset, 
                        startStoryTextWidth - characterImage.rectTransform.rect.width);
                }
            }
        }

        /// <summary>
        /// Sets which dialog box background and text layout side to display.
        /// </summary>
        public virtual void SetDialogSide(DialogSide side)
        {
            if (panelImage == null && panelImageRight == null)
            {
                return;
            }

            bool isRight = (side == DialogSide.Right);

            // Toggle the correct dialog box background
            if (panelImage != null)
            {
                panelImage.gameObject.SetActive(!isRight);
            }
            if (panelImageRight != null)
            {
                panelImageRight.gameObject.SetActive(isRight);
            }

            // Mirror the text layout so it stays inside the active dialog box.
            // Layout is authored for the left side; mirroring around the panel
            // center flips it to the right side.
            MirrorTextLayout(isRight);

            // Flip the character image horizontally so it faces the dialog box.
            if (characterImage != null)
            {
                Vector3 scale = characterImage.rectTransform.localScale;
                scale.x = Mathf.Abs(scale.x) * (isRight ? -1f : 1f);
                characterImage.rectTransform.localScale = scale;
            }
        }

        /// <summary>
        /// Mirrors the anchored positions of name, subtitle, story text and continue button
        /// so the layout looks correct on both sides of the dialog box.
        /// </summary>
        protected virtual void MirrorTextLayout(bool isRight)
        {
            MirrorHorizontal(characterImage != null ? characterImage.rectTransform : null, isRight);
            MirrorHorizontal(nameText != null ? nameText.rectTransform : null, isRight);
            MirrorHorizontal(subtitleText != null ? subtitleText.rectTransform : null, isRight);
            MirrorHorizontal(storyText != null ? storyText.rectTransform : null, isRight);
            MirrorHorizontal(continueButton != null ? continueButton.GetComponent<RectTransform>() : null, isRight);
        }

        /// <summary>
        /// Mirrors a RectTransform horizontally around the center of its parent.
        /// The original (left side) layout is cached on first use so repeated calls
        /// do not accumulate the mirror transformation.
        /// </summary>
        protected virtual void MirrorHorizontal(RectTransform rectTransform, bool isRight)
        {
            if (rectTransform == null)
            {
                return;
            }

            // Cache the authored layout on first use.
            if (!mirroredLayoutCache.ContainsKey(rectTransform))
            {
                mirroredLayoutCache[rectTransform] = new RectLayoutCache(
                    rectTransform.anchorMin, rectTransform.anchorMax, rectTransform.anchoredPosition);
            }

            var cached = mirroredLayoutCache[rectTransform];

            if (isRight)
            {
                rectTransform.anchorMin = new Vector2(1f - cached.anchorMax.x, cached.anchorMin.y);
                rectTransform.anchorMax = new Vector2(1f - cached.anchorMin.x, cached.anchorMax.y);
                rectTransform.anchoredPosition = new Vector2(-cached.anchoredPosition.x, cached.anchoredPosition.y);
            }
            else
            {
                rectTransform.anchorMin = cached.anchorMin;
                rectTransform.anchorMax = cached.anchorMax;
                rectTransform.anchoredPosition = cached.anchoredPosition;
            }
        }

        /// <summary>
        /// Sets the dialog box background sprite. Pass null to restore the default panel sprite.
        /// </summary>
        public virtual void SetPanelImage(Sprite sprite)
        {
            if (panelImage == null)
            {
                return;
            }

            panelImage.sprite = sprite != null ? sprite : defaultPanelSprite;

            if (panelImageRight != null)
            {
                panelImageRight.sprite = sprite != null ? sprite : defaultPanelSpriteRight;
            }
        }

        /// <summary>
        /// Sets the character name to display on the Say Dialog.
        /// Supports variable substitution e.g. John {$surname}
        /// </summary>
        public virtual void SetCharacterName(string name, Color color)
        {
            if (NameText != null)
            {
                var subbedName = stringSubstituter.SubstituteStrings(name);
                NameText = subbedName;
                nameTextAdapter.SetTextColor(color);
            }
        }

        /// <summary>
        /// Write a line of story text to the Say Dialog. Starts coroutine automatically.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="clearPrevious">Clear any previous text in the Say Dialog.</param>
        /// <param name="waitForInput">Wait for player input before continuing once text is written.</param>
        /// <param name="fadeWhenDone">Fade out the Say Dialog when writing and player input has finished.</param>
        /// <param name="stopVoiceover">Stop any existing voiceover audio before writing starts.</param>
        /// <param name="voiceOverClip">Voice over audio clip to play.</param>
        /// <param name="onComplete">Callback to execute when writing and player input have finished.</param>
        public virtual void Say(string text, bool clearPrevious, bool waitForInput, bool fadeWhenDone, bool stopVoiceover, bool waitForVO, AudioClip voiceOverClip, Action onComplete)
        {
            StartCoroutine(DoSay(text, clearPrevious, waitForInput, fadeWhenDone, stopVoiceover, waitForVO, voiceOverClip, onComplete));
        }

        /// <summary>
        /// Write a line of story text to the Say Dialog. Must be started as a coroutine.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="clearPrevious">Clear any previous text in the Say Dialog.</param>
        /// <param name="waitForInput">Wait for player input before continuing once text is written.</param>
        /// <param name="fadeWhenDone">Fade out the Say Dialog when writing and player input has finished.</param>
        /// <param name="stopVoiceover">Stop any existing voiceover audio before writing starts.</param>
        /// <param name="voiceOverClip">Voice over audio clip to play.</param>
        /// <param name="onComplete">Callback to execute when writing and player input have finished.</param>
        public virtual IEnumerator DoSay(string text, bool clearPrevious, bool waitForInput, bool fadeWhenDone, bool stopVoiceover, bool waitForVO, AudioClip voiceOverClip, Action onComplete)
        {
            var writer = GetWriter();

            if (writer.IsWriting || writer.IsWaitingForInput)
            {
                writer.Stop();
                while (writer.IsWriting || writer.IsWaitingForInput)
                {
                    yield return null;
                }
            }

            if (closeOtherDialogs)
            {
                for (int i = 0; i < activeSayDialogs.Count; i++)
                {
                    var sd = activeSayDialogs[i];
                    if (sd.gameObject != gameObject)
                    {
                        sd.SetActive(false);
                    }
                }
            }
            gameObject.SetActive(true);

            this.fadeWhenDone = fadeWhenDone;

            // Voice over clip takes precedence over a character sound effect if provided

            AudioClip soundEffectClip = null;
            if (voiceOverClip != null)
            {
                WriterAudio writerAudio = GetWriterAudio();
                writerAudio.OnVoiceover(voiceOverClip);
            }
            else if (speakingCharacter != null)
            {
                soundEffectClip = speakingCharacter.SoundEffect;
            }

            writer.AttachedWriterAudio = writerAudio;

            yield return StartCoroutine(writer.Write(text, clearPrevious, waitForInput, stopVoiceover, waitForVO, soundEffectClip, onComplete));
        }

        /// <summary>
        /// Tell the Say Dialog to fade out once writing and player input have finished.
        /// </summary>
        public virtual bool FadeWhenDone { get {return fadeWhenDone; } set { fadeWhenDone = value; } }

        /// <summary>
        /// Stop the Say Dialog while its writing text.
        /// </summary>
        public virtual void Stop()
        {
            fadeWhenDone = true;
            GetWriter().Stop();
        }

        /// <summary>
        /// Stops writing text and clears the Say Dialog.
        /// </summary>
        public virtual void Clear()
        {
            ClearStoryText();

            // Kill any active write coroutine
            StopAllCoroutines();
        }

        #endregion
    }
}

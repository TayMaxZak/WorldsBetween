using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIKeystone : MonoBehaviour
{
	[SerializeField]
	private InputField sourceField;
	[SerializeField]
	private Text displayText;
	[SerializeField]
	private Text placeholderText;
	[Header("")]
	[SerializeField]
	private string currentStringSeed = "000000000";
	[SerializeField]
	private string defaultStringSeed = "ENTERCODE";
	private bool isDefaultSeed = true;

	[Header("")]
	[SerializeField]
	private Animation shuffleAnim;

	private Timer shuffleTimer = new Timer(10f);
	private float shuffleTimeMin = 8f;
	private float shuffleTimeMax = 12f;

	private void Awake()
	{
		isDefaultSeed = true;
		currentStringSeed = defaultStringSeed;
		ApplyKeyCode(defaultStringSeed, false, true);
	}

	private void Update()
	{
		shuffleTimer.Increment(Time.deltaTime);

		// Not on screen (or just leaving this screen)
		if (MainMenu.Instance.state != MainMenu.MainMenuState.NewGame)
		{
			// While not in view, return to default code (if needed)
			if (!isDefaultSeed && shuffleTimer.Expired())
			{
				shuffleTimer.Reset();

				PlayShuffleAnim(); // In case player catches a glimpse

				isDefaultSeed = true;
				currentStringSeed = defaultStringSeed;
				ApplyKeyCode(defaultStringSeed, false, true);

			}
		}
		// While a seed is not manually entered
		else if (!sourceField.isFocused && sourceField.text.Length == 0)
		{
			// Periodically replace the default code
			if (shuffleTimer.Expired())
			{
				shuffleTimer.Reset(SeedlessRandom.NextFloatInRange(shuffleTimeMin, shuffleTimeMax));

				// While in view, fully shuffle
				if (MainMenu.Instance.state == MainMenu.MainMenuState.NewGame)
				{
					isDefaultSeed = false;
					currentStringSeed = CreateRandomKeyCode();
					ApplyKeyCode(currentStringSeed, false, true);
				}
			}
		}
		// Still highlighted, so suppress shuffling and keep timer at max
		else
		{
			shuffleTimer.Reset();
		}
	}

	private string CreateRandomKeyCode()
	{
		string toReturn = "";

		for (int i = 0; i < 9; i++)
		{
			toReturn += CreateRandomChar();
		}

		PlayShuffleAnim();

		return toReturn;
	}

	private void PlayShuffleAnim()
	{
		if (shuffleAnim)
		{
			if (shuffleAnim.isPlaying)
				shuffleAnim.Stop();
			shuffleAnim.Play();
		}
	}

	private char CreateRandomChar()
	{
		return SeedDecoder.CharOfValue((byte)SeedlessRandom.NextIntInRange(0, 36));
	}

	// Pick opposite type of char
	private char CreateRandomChar(char source)
	{
		if (char.IsDigit(source))
			return SeedDecoder.CharOfValue((byte)SeedlessRandom.NextIntInRange(10, 36));
		else if (char.IsLetter(source))
			return SeedDecoder.CharOfValue((byte)SeedlessRandom.NextIntInRange(0, 9));
		else
			return '#';
	}

	public void ConfirmKeyCode()
	{
		string sourceText = sourceField.text;

		if (sourceText.Length > 0)
			ApplyKeyCode(sourceText, true, false);
		else
			ApplyKeyCode(CreateRandomKeyCode(), true, true);
	}

	// TODO: Optimize with string builder?
	private void ApplyKeyCode(string sourceText, bool updateStringSeed, bool noDisplayText)
	{
		string toDisplay = "";
		string toPlaceholder = "";

		if (updateStringSeed)
			currentStringSeed = "";

		// Display text includes whitespace chars
		for (int i = 0; i < 9; i++)
		{
			if (i < sourceText.Length)
			{
				toDisplay += char.ToUpper(sourceText[i]) + " ";

				// Use placeholder text as display text
				if (noDisplayText)
					toPlaceholder += char.ToUpper(sourceText[i]) + " ";
				// Whitespace
				else
					toPlaceholder += (i + 1) % 3 == 0 ? "\n" : "  ";

				if (updateStringSeed)
					currentStringSeed += char.ToUpper(sourceText[i]);
			}
			else
			{
				toPlaceholder += "0 ";

				if (updateStringSeed)
					currentStringSeed += '0';
			}
		}

		if (noDisplayText)
		{
			placeholderText.text = toPlaceholder;

			if (sourceField.text.Length == 0)
				displayText.text = "";
		}
		else
		{
			placeholderText.text = toPlaceholder;

			displayText.text = toDisplay;
		}
	}

	public string GetStringSeed()
	{
		return currentStringSeed;
	}
}

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
	[SerializeField]
	private string finalStringSeed = "000000000";

	[Header("")]
	[SerializeField]
	private Animation shuffleAnim;

	private Timer shuffleTimer = new Timer(10f);
	private float shuffleTimeMin = 8f;
	private float shuffleTimeMax = 12f;

	private void Awake()
	{
		ApplyKeyCode("ENTERCODE", true, true);
	}

	private void Update()
	{
		if (MainMenu.Instance.state != MainMenu.MainMenuState.NewGame)
			return;

		// While a seed is not manually entered
		if (!sourceField.isFocused && sourceField.text.Length == 0)
		{
			// Replace a random char
			shuffleTimer.Increment(Time.deltaTime);
			if (shuffleTimer.Expired())
			{
				shuffleTimer.Reset(SeedlessRandom.NextFloatInRange(shuffleTimeMin, shuffleTimeMax));

				ApplyKeyCode(CreateRandomKeyCode(), true, true);
			}
		}
		else
			shuffleTimer.Reset();
	}

	private string CreateRandomKeyCode()
	{
		string toReturn = "";

		for (int i = 0; i < 9; i++)
		{
			toReturn += CreateRandomChar();
		}

		if (shuffleAnim)
		{
			if (shuffleAnim.isPlaying)
				shuffleAnim.Stop();
			shuffleAnim.Play();
		}

		return toReturn;
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
			finalStringSeed = "";

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
					finalStringSeed += char.ToUpper(sourceText[i]);
			}
			else
			{
				toPlaceholder += "0 ";

				if (updateStringSeed)
					finalStringSeed += '0';
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
		return finalStringSeed;
	}
}

﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;

using Plugin.Media;
using Plugin.Media.Abstractions;

using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;

using Xamarin;
using Xamarin.Forms;

namespace FaceOff
{
	public class PictureViewModel : BaseViewModel
	{
		#region Constant Fields
		readonly string[] _emotionStrings = { "Anger", "Contempt", "Disgust", "Fear", "Happiness", "Neutral", "Sadness", "Surprise" };
		readonly string[] _emotionStringsForAlertMessage = { "angry", "disrespectful", "disgusted", "scared", "happy", "blank", "sad", "surprised" };

		const string _makeAFaceAlertMessage = "take a selfie looking ";
		const string _calculatingScoreMessage = "Analyzing";

		const string _playerNumberNotImplentedExceptionText = "Player Number Not Implemented";

		readonly Dictionary<ErrorMessageType, string> _errorMessageDictionary = new Dictionary<ErrorMessageType, string>
		{
			{ ErrorMessageType.NoFaceDetected, "No Face Detected" },
			{ ErrorMessageType.MultipleFacesDetected, "Multiple Faces Detected" },
			{ ErrorMessageType.GenericError, "Error" }
		};
		#endregion

		#region Fields
		ImageSource _photo1ImageSource, _photo2ImageSource;
		string _scoreButton1Text, _scoreButton2Text;
		bool _isTakeLeftPhotoButtonEnabled = true;
		bool _isTakeLeftPhotoButtonStackVisible = true;
		bool _isTakeRightPhotoButtonEnabled = true;
		bool _isTakeRightPhotoButtonStackVisible = true;
		bool _isResetButtonEnabled;
		string _pageTitle;
		int _emotionNumber;
		bool _isCalculatingPhoto1Score, _isCalculatingPhoto2Score;
		bool _isScore1ButtonEnabled, _isScore2ButtonEnabled, _isScore1ButtonVisable, _isScore2ButtonVisable;
		bool _isPhotoImage1Enabled, _isPhotoImage2Enabled;
		string _photo1Results, _photo2Results;
		ICommand _takePhoto1ButtonPressed, _takePhoto2ButtonPressed;
		ICommand _photo1ScoreButtonPressed, _photo2ScoreButtonPressed;
		ICommand _resetButtonPressed;

		public event EventHandler<AlertMessageEventArgs> DisplayEmotionBeforeCameraAlert;
		public event EventHandler<TextEventArgs> DisplayAllEmotionResultsAlert;
		public event EventHandler DisplayMultipleFacesDetectedAlert;
		public event EventHandler DisplayNoCameraAvailableAlert;
		public event EventHandler RevealScoreButton1WithAnimation;
		public event EventHandler RevealScoreButton2WithAnimation;
		public event EventHandler RevealPhotoImage1WithAnimation;
		public event EventHandler RevealPhotoImage2WithAnimation;
		#endregion

		#region Constructors
		public PictureViewModel()
		{
			IsResetButtonEnabled = false;

			SetEmotion();
		}
		#endregion

		#region Properties
		public ICommand TakePhoto1ButtonPressed =>
		_takePhoto1ButtonPressed ??
		(_takePhoto1ButtonPressed = new Command(async () => await ExecuteTakePhoto1ButtonPressed()));

		public ICommand TakePhoto2ButtonPressed =>
		_takePhoto2ButtonPressed ??
		(_takePhoto2ButtonPressed = new Command(async () => await ExecuteTakePhoto2ButtonPressed()));

		public ICommand ResetButtonPressed =>
		_resetButtonPressed ??
		(_resetButtonPressed = new Command(ExecuteResetButtonPressed));

		public ICommand Photo1ScoreButtonPressed =>
		_photo1ScoreButtonPressed ??
		(_photo1ScoreButtonPressed = new Command(ExecutePhoto1ScoreButtonPressed));

		public ICommand Photo2ScoreButtonPressed =>
		 _photo2ScoreButtonPressed ??
		 (_photo2ScoreButtonPressed = new Command(ExecutePhoto2ScoreButtonPressed));

		public ImageSource Photo1ImageSource
		{
			get { return _photo1ImageSource; }
			set { SetProperty(ref _photo1ImageSource, value); }
		}

		public ImageSource Photo2ImageSource
		{
			get { return _photo2ImageSource; }
			set { SetProperty(ref _photo2ImageSource, value); }
		}

		public bool IsPhotoImage1Enabled
		{
			get { return _isPhotoImage1Enabled; }
			set { SetProperty(ref _isPhotoImage1Enabled, value); }
		}

		public bool IsPhotoImage2Enabled
		{
			get { return _isPhotoImage2Enabled; }
			set { SetProperty(ref _isPhotoImage2Enabled, value); }
		}

		public bool IsTakeLeftPhotoButtonEnabled
		{
			get { return _isTakeLeftPhotoButtonEnabled; }
			set { SetProperty(ref _isTakeLeftPhotoButtonEnabled, value); }
		}

		public bool IsTakeLeftPhotoButtonStackVisible
		{
			get { return _isTakeLeftPhotoButtonStackVisible; }
			set { SetProperty(ref _isTakeLeftPhotoButtonStackVisible, value); }
		}

		public bool IsTakeRightPhotoButtonEnabled
		{
			get { return _isTakeRightPhotoButtonEnabled; }
			set { SetProperty(ref _isTakeRightPhotoButtonEnabled, value); }
		}

		public bool IsTakeRightPhotoButtonStackVisible
		{
			get { return _isTakeRightPhotoButtonStackVisible; }
			set { SetProperty(ref _isTakeRightPhotoButtonStackVisible, value); }
		}

		public string PageTitle
		{
			get { return _pageTitle; }
			set { SetProperty(ref _pageTitle, value); }
		}

		public string ScoreButton1Text
		{
			get { return _scoreButton1Text; }
			set { SetProperty(ref _scoreButton1Text, value); }
		}

		public string ScoreButton2Text
		{
			get { return _scoreButton2Text; }
			set { SetProperty(ref _scoreButton2Text, value); }
		}

		public bool IsCalculatingPhoto1Score
		{
			get { return _isCalculatingPhoto1Score; }
			set { SetProperty(ref _isCalculatingPhoto1Score, value); }
		}

		public bool IsCalculatingPhoto2Score
		{
			get { return _isCalculatingPhoto2Score; }
			set { SetProperty(ref _isCalculatingPhoto2Score, value); }
		}

		public bool IsResetButtonEnabled
		{
			get { return _isResetButtonEnabled; }
			set { SetProperty(ref _isResetButtonEnabled, value); }
		}

		public bool IsScore1ButtonEnabled
		{
			get { return _isScore1ButtonEnabled; }
			set { SetProperty(ref _isScore1ButtonEnabled, value); }
		}

		public bool IsScore2ButtonEnabled
		{
			get { return _isScore2ButtonEnabled; }
			set { SetProperty(ref _isScore2ButtonEnabled, value); }
		}

		public bool IsScore1ButtonVisable
		{
			get { return _isScore1ButtonVisable; }
			set { SetProperty(ref _isScore1ButtonVisable, value); }
		}

		public bool IsScore2ButtonVisable
		{
			get { return _isScore2ButtonVisable; }
			set { SetProperty(ref _isScore2ButtonVisable, value); }
		}

		public bool HasUserAcknowledgedPopUp { get; set; } = false;
		public bool UserResponseToAlert { get; set; }
		#endregion

		#region Methods
		public void SetPhotoImage1(string photo1ImageString)
		{
			Photo1ImageSource = photo1ImageString;

			var allEmotionsString = "";
			allEmotionsString += $"Anger: 0%\n";
			allEmotionsString += $"Contempt: 0%\n";
			allEmotionsString += $"Disgust: 0%\n";
			allEmotionsString += $"Fear: 0%\n";
			allEmotionsString += $"Happiness: 100%\n";
			allEmotionsString += $"Neutral: 0%\n";
			allEmotionsString += $"Sadness: 0%\n";
			allEmotionsString += $"Surprise: 0%";

			_photo1Results = allEmotionsString;
			ScoreButton1Text = "Score: 100%";

			SetEmotion(4);

			IsTakeLeftPhotoButtonEnabled = false;
			IsTakeLeftPhotoButtonStackVisible = false;

			OnRevealPhotoImage1WithAnimation();
			OnRevealScoreButton1WithAnimation();
		}

		public void SetPhotoImage2(string photo2ImageString)
		{
			Photo2ImageSource = photo2ImageString;

			var allEmotionsString = "";
			allEmotionsString += $"Anger: 0%\n";
			allEmotionsString += $"Contempt: 0%\n";
			allEmotionsString += $"Disgust: 0%\n";
			allEmotionsString += $"Fear: 0%\n";
			allEmotionsString += $"Happiness: 100%\n";
			allEmotionsString += $"Neutral: 0%\n";
			allEmotionsString += $"Sadness: 0%\n";
			allEmotionsString += $"Surprise: 0%";

			_photo2Results = allEmotionsString;
			ScoreButton2Text = "Score: 100%";

			SetEmotion(4);

			IsTakeRightPhotoButtonEnabled = false;
			IsTakeRightPhotoButtonStackVisible = false;

			OnRevealPhotoImage2WithAnimation();
			OnRevealScoreButton2WithAnimation();
		}

		async Task ExecuteTakePhoto1ButtonPressed()
		{
			await ExecuteTakePhoto(new PlayerModel(PlayerNumberType.Player1, Settings.Player1Name));
		}

		async Task ExecuteTakePhoto2ButtonPressed()
		{
			await ExecuteTakePhoto(new PlayerModel(PlayerNumberType.Player2, Settings.Player2Name));
		}

		async Task ExecuteTakePhoto(PlayerModel playerModel)
		{
			LogPhotoButtonTapped(playerModel.PlayerNumber);

			DisableButtons(playerModel.PlayerNumber);

			if (!(await DisplayPopUpAlertAboutEmotion(playerModel.PlayerName)))
			{
				EnableButtons(playerModel.PlayerNumber);
				return;
			}

			playerModel.ImageMediaFile = await GetMediaFileFromCamera("FaceOff", playerModel.PlayerNumber);

			if (playerModel.ImageMediaFile == null)
			{
				EnableButtons(playerModel.PlayerNumber);
				return;
			}

			await WaitForRevealPhotoImageWithAnimationEventToBeSubscribed(playerModel.PlayerNumber);

			RevealPhoto(playerModel.PlayerNumber);

			Insights.Track(InsightsConstants.PhotoTaken);

			SetIsEnabledForCurrentPhotoButton(false, playerModel.PlayerNumber);
			SetIsVisibleForCurrentPhotoStack(false, playerModel.PlayerNumber);

			SetScoreButtonText(_calculatingScoreMessage, playerModel.PlayerNumber);

			SetPhotoImageSource(playerModel.ImageMediaFile, playerModel.PlayerNumber);

			SetIsCalculatingPhotoScore(true, playerModel.PlayerNumber);

			SetResetButtonIsEnabled();

			await WaitForAnimationsToFinish((int)Math.Ceiling(AnimationConstants.PhotoImageAninmationTime * 2.5));

			await WaitForRevealScoreButtonWithAnimationEventToBeSubscribed(playerModel.PlayerNumber);

			RevealPhotoButton(playerModel.PlayerNumber);

			var emotionArray = await GetEmotionResultsFromMediaFile(playerModel.ImageMediaFile, false);

			var emotionScore = GetPhotoEmotionScore(emotionArray, 0);

			var doesEmotionScoreContainErrorMessage = DoesStringContainErrorMessage(emotionScore);

			if (doesEmotionScoreContainErrorMessage)
			{
				var errorMessageKey = _errorMessageDictionary.FirstOrDefault(x => x.Value.Contains(emotionScore)).Key;

				switch (errorMessageKey)
				{
					case ErrorMessageType.NoFaceDetected:
						Insights.Track(_errorMessageDictionary[ErrorMessageType.NoFaceDetected]);
						break;
					case ErrorMessageType.MultipleFacesDetected:
						Insights.Track(_errorMessageDictionary[ErrorMessageType.MultipleFacesDetected]);
						break;
					case ErrorMessageType.GenericError:
						Insights.Track(_errorMessageDictionary[ErrorMessageType.MultipleFacesDetected]);
						break;
				}

				SetScoreButtonText(emotionScore, playerModel.PlayerNumber);
			}
			else
				SetScoreButtonText($"Score: {emotionScore}", playerModel.PlayerNumber);

			var results = GetStringOfAllPhotoEmotionScores(emotionArray, 0);
			SetPhotoResultsText(results, playerModel.PlayerNumber);

			SetIsCalculatingPhotoScore(false, playerModel.PlayerNumber);

			SetResetButtonIsEnabled();

			playerModel.ImageMediaFile.Dispose();

			await WaitForAnimationsToFinish((int)Math.Ceiling(AnimationConstants.ScoreButonAninmationTime * 2.5));
		}

		void ExecuteResetButtonPressed()
		{
			Insights.Track(InsightsConstants.ResetButtonTapped);

			SetEmotion();

			Photo1ImageSource = null;
			Photo2ImageSource = null;

			IsTakeLeftPhotoButtonEnabled = true;
			IsTakeLeftPhotoButtonStackVisible = true;

			IsTakeRightPhotoButtonEnabled = true;
			IsTakeRightPhotoButtonStackVisible = true;

			ScoreButton1Text = null;
			ScoreButton2Text = null;

			IsScore1ButtonEnabled = false;
			IsScore2ButtonEnabled = false;

			IsScore1ButtonVisable = false;
			IsScore2ButtonVisable = false;

			_photo1Results = null;
			_photo2Results = null;

			IsPhotoImage1Enabled = false;
			IsPhotoImage2Enabled = false;
		}

		void ExecutePhoto1ScoreButtonPressed()
		{
			Insights.Track(InsightsConstants.ResultsButton1Tapped);
			OnDisplayAllEmotionResultsAlert(_photo1Results);
		}

		void ExecutePhoto2ScoreButtonPressed()
		{
			Insights.Track(InsightsConstants.ResultsButton2Tapped);
			OnDisplayAllEmotionResultsAlert(_photo2Results);
		}

		Stream GetPhotoStream(MediaFile mediaFile, bool disposeMediaFile)
		{
			var stream = mediaFile.GetStream();

			if (disposeMediaFile)
				mediaFile.Dispose();

			return stream;
		}

		async Task<MediaFile> GetMediaFileFromCamera(string directory, PlayerNumberType playerNumber)
		{
			await CrossMedia.Current.Initialize();

			if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
			{
				OnDisplayNoCameraAvailableAlert();
				return null;
			}

			var file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
			{
				PhotoSize = PhotoSize.Small,
				Directory = directory,
				Name = playerNumber.ToString(),
				DefaultCamera = CameraDevice.Front,
				OverlayViewProvider = DependencyService.Get<ICameraService>()?.GetCameraOverlay()
			});

			return file;
		}

		async Task<Emotion[]> GetEmotionResultsFromMediaFile(MediaFile mediaFile, bool disposeMediaFile)
		{
			if (mediaFile == null)
				return null;

			try
			{
				var emotionClient = new EmotionServiceClient(CognitiveServicesConstants.EmotionApiKey);

				using (var handle = Insights.TrackTime(InsightsConstants.AnalyzeEmotion))
				{
					return await emotionClient.RecognizeAsync(GetPhotoStream(mediaFile, disposeMediaFile));
				}
			}
			catch (Exception e)
			{
				Insights.Report(e);
				return null;
			}
		}

		int GetRandomNumberForEmotion()
		{
			int randomNumber;

			do
			{
				var rnd = new Random();
				randomNumber = rnd.Next(0, _emotionStrings.Length);
			} while (randomNumber == _emotionNumber);

			return randomNumber;
		}

		void SetPageTitle(int emotionNumber)
		{
			PageTitle = _emotionStrings[emotionNumber];
		}

		void SetEmotion(int? emotionNumber = null)
		{
			if (emotionNumber != null && emotionNumber >= 0 && emotionNumber <= _emotionStrings.Length - 1)
				_emotionNumber = (int)emotionNumber;
			else
				_emotionNumber = GetRandomNumberForEmotion();

			SetPageTitle(_emotionNumber);
		}

		string GetPhotoEmotionScore(Emotion[] emotionResults, int emotionResultNumber)
		{
			float rawEmotionScore;

			if (emotionResults == null || emotionResults.Length < 1)
				return _errorMessageDictionary[ErrorMessageType.NoFaceDetected];

			if (emotionResults.Length > 1)
			{
				OnDisplayMultipleFacesError();
				return _errorMessageDictionary[ErrorMessageType.MultipleFacesDetected];
			}

			try
			{
				switch (_emotionNumber)
				{
					case 0:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Anger;
						break;
					case 1:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Contempt;
						break;
					case 2:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Disgust;
						break;
					case 3:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Fear;
						break;
					case 4:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Happiness;
						break;
					case 5:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Neutral;
						break;
					case 6:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Sadness;
						break;
					case 7:
						rawEmotionScore = emotionResults[emotionResultNumber].Scores.Surprise;
						break;
					default:
						return _errorMessageDictionary[ErrorMessageType.GenericError];
				}

				var emotionScoreAsPercentage = ConvertFloatToPercentage(rawEmotionScore);

				return emotionScoreAsPercentage;
			}
			catch (Exception e)
			{
				Insights.Report(e);
				return _errorMessageDictionary[ErrorMessageType.GenericError];
			}
		}

		string GetStringOfAllPhotoEmotionScores(Emotion[] emotionResults, int emotionResultNumber)
		{
			if (emotionResults == null || emotionResults.Length < 1)
				return _errorMessageDictionary[ErrorMessageType.GenericError];

			string allEmotionsString = "";

			allEmotionsString += $"Anger: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Anger)}\n";
			allEmotionsString += $"Contempt: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Contempt)}\n";
			allEmotionsString += $"Disgust: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Disgust)}\n";
			allEmotionsString += $"Fear: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Fear)}\n";
			allEmotionsString += $"Happiness: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Happiness)}\n";
			allEmotionsString += $"Neutral: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Neutral)}\n";
			allEmotionsString += $"Sadness: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Sadness)}\n";
			allEmotionsString += $"Surprise: {ConvertFloatToPercentage(emotionResults[emotionResultNumber].Scores.Surprise)}";

			return allEmotionsString;
		}

		string ConvertFloatToPercentage(float floatToConvert)
		{
			return floatToConvert.ToString("#0.##%");
		}

		async Task<bool> DisplayPopUpAlertAboutEmotion(string playerName)
		{
			var alertMessage = new AlertMessageModel
			{
				Title = _emotionStrings[_emotionNumber],
				Message = playerName + ", " + _makeAFaceAlertMessage + _emotionStringsForAlertMessage[_emotionNumber]
			};
			OnDisplayEmotionBeforeCameraAlert(alertMessage);

			while (!HasUserAcknowledgedPopUp)
			{
				await Task.Delay(5);
			}
			HasUserAcknowledgedPopUp = false;

			return UserResponseToAlert;
		}

		bool DoesStringContainErrorMessage(string stringToCheck)
		{
			foreach (KeyValuePair<ErrorMessageType, string> errorMessageDictionaryEntry in _errorMessageDictionary)
			{
				if (stringToCheck.Contains(errorMessageDictionaryEntry.Value))
					return true;
			}

			return false;
		}

		async Task WaitForEventToBeSubscribed(EventHandler eventToWaitFor)
		{
			while (eventToWaitFor == null)
				await Task.Delay(100);
		}

		async Task WaitForRevealPhotoImageWithAnimationEventToBeSubscribed(PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					await WaitForEventToBeSubscribed(RevealPhotoImage1WithAnimation);
					break;
				case PlayerNumberType.Player2:
					await WaitForEventToBeSubscribed(RevealPhotoImage2WithAnimation);
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		async Task WaitForAnimationsToFinish(int waitTimeInSeconds)
		{
			await Task.Delay(waitTimeInSeconds);
		}

		void RevealPhoto(PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					OnRevealPhotoImage1WithAnimation();
					break;
				case PlayerNumberType.Player2:
					OnRevealPhotoImage2WithAnimation();
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetIsEnabledForOppositePhotoButton(bool isEnabled, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					IsTakeRightPhotoButtonEnabled = isEnabled;
					break;
				case PlayerNumberType.Player2:
					IsTakeLeftPhotoButtonEnabled = isEnabled;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetIsEnabledForCurrentPlayerScoreButton(bool isEnabled, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					IsScore1ButtonEnabled = isEnabled;
					break;
				case PlayerNumberType.Player2:
					IsScore2ButtonEnabled = isEnabled;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetIsEnabledForButtons(bool isEnabled, PlayerNumberType playerNumber)
		{
			SetIsEnabledForOppositePhotoButton(isEnabled, playerNumber);
			SetIsEnabledForCurrentPlayerScoreButton(isEnabled, playerNumber);
		}

		void EnableButtons(PlayerNumberType playerNumber)
		{
			SetIsEnabledForButtons(true, playerNumber);
		}

		void DisableButtons(PlayerNumberType playerNumber)
		{
			SetIsEnabledForButtons(false, playerNumber);
		}

		void SetIsEnabledForCurrentPhotoButton(bool isEnabled, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					IsTakeLeftPhotoButtonEnabled = isEnabled;
					break;
				case PlayerNumberType.Player2:
					IsTakeRightPhotoButtonEnabled = isEnabled;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetScoreButtonText(string scoreButtonText, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					ScoreButton1Text = scoreButtonText;
					break;
				case PlayerNumberType.Player2:
					ScoreButton2Text = scoreButtonText;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetIsVisibleForCurrentPhotoStack(bool isVisible, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					IsTakeLeftPhotoButtonStackVisible = isVisible;
					break;
				case PlayerNumberType.Player2:
					IsTakeRightPhotoButtonStackVisible = isVisible;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetPhotoImageSource(MediaFile imageMediaFile, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					Photo1ImageSource = ImageSource.FromStream(() =>
					{
						return GetPhotoStream(imageMediaFile, false);
					});
					break;
				case PlayerNumberType.Player2:
					Photo2ImageSource = ImageSource.FromStream(() =>
					{
						return GetPhotoStream(imageMediaFile, false);
					});
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetIsCalculatingPhotoScore(bool isCalculatingScore, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					IsCalculatingPhoto1Score = isCalculatingScore;
					break;
				case PlayerNumberType.Player2:
					IsCalculatingPhoto2Score = isCalculatingScore;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetResetButtonIsEnabled()
		{
			IsResetButtonEnabled = !(IsCalculatingPhoto1Score || IsCalculatingPhoto2Score);
		}

		async Task WaitForRevealScoreButtonWithAnimationEventToBeSubscribed(PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					await WaitForEventToBeSubscribed(RevealScoreButton1WithAnimation);
					break;
				case PlayerNumberType.Player2:
					await WaitForEventToBeSubscribed(RevealScoreButton2WithAnimation);
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void RevealPhotoButton(PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					OnRevealScoreButton1WithAnimation();
					break;
				case PlayerNumberType.Player2:
					OnRevealScoreButton2WithAnimation();
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void SetPhotoResultsText(string results, PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					_photo1Results = results;
					break;
				case PlayerNumberType.Player2:
					_photo2Results = results;
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void LogPhotoButtonTapped(PlayerNumberType playerNumber)
		{
			switch (playerNumber)
			{
				case PlayerNumberType.Player1:
					Insights.Track(InsightsConstants.PhotoButton1Tapped);
					break;
				case PlayerNumberType.Player2:
					Insights.Track(InsightsConstants.PhotoButton2Tapped);
					break;
				default:
					throw new Exception(_playerNumberNotImplentedExceptionText);
			}
		}

		void OnDisplayAllEmotionResultsAlert(string emotionResults)
		{
			var handle = DisplayAllEmotionResultsAlert;
			handle?.Invoke(null, new TextEventArgs(emotionResults));
		}

		void OnDisplayNoCameraAvailableAlert()
		{
			var handle = DisplayNoCameraAvailableAlert;
			handle?.Invoke(null, EventArgs.Empty);
		}

		void OnDisplayEmotionBeforeCameraAlert(AlertMessageModel alertMessage)
		{
			var handle = DisplayEmotionBeforeCameraAlert;
			handle?.Invoke(null, new AlertMessageEventArgs(alertMessage));
		}

		void OnRevealPhotoImage1WithAnimation()
		{
			var handle = RevealPhotoImage1WithAnimation;
			handle?.Invoke(null, EventArgs.Empty);
		}

		void OnRevealScoreButton1WithAnimation()
		{
			var handle = RevealScoreButton1WithAnimation;
			handle?.Invoke(null, EventArgs.Empty); ;
		}

		void OnRevealPhotoImage2WithAnimation()
		{
			var handle = RevealPhotoImage2WithAnimation;
			handle?.Invoke(null, EventArgs.Empty);
		}

		void OnRevealScoreButton2WithAnimation()
		{
			var handle = RevealScoreButton2WithAnimation;
			handle?.Invoke(null, EventArgs.Empty);
		}

		void OnDisplayMultipleFacesError()
		{
			var handle = DisplayMultipleFacesDetectedAlert;
			handle?.Invoke(null, EventArgs.Empty);
		}
		#endregion
	}

	enum ErrorMessageType
	{
		NoFaceDetected,
		MultipleFacesDetected,
		GenericError
	}
}


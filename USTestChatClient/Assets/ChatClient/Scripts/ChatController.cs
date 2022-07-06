using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace USTestChat.Client
{
	class ChatController : MonoBehaviour
	{
		// ====================================================================
		// Inspector fields
		// ====================================================================

#pragma warning disable 0649
		[SerializeField] string ServerHost = "localhost";
		[SerializeField] int ServerPort = 6677;

		[SerializeField] RectTransform panelLogin;
		[SerializeField] TMP_InputField inputFieldName;
		[SerializeField] FlexibleColorPicker colorPicker;
		[SerializeField] Button buttonConnect;

		[SerializeField] ScrollRect scrollView;
		[SerializeField] TMP_InputField inputFieldChatText;
		[SerializeField] TMP_Text textScrollViewContent;
#pragma warning restore 0649

		// ====================================================================
		// Private data
		// ====================================================================

		Telepathy.Client _client;

		// ====================================================================
		// Unity callbacks
		// ====================================================================

		void Awake()
		{
			buttonConnect.onClick.AddListener(OnButtonConnectClick);
			inputFieldChatText.onEndEdit.AddListener(OnInputFieldEndEdit);

			// update even if window isn't focused, otherwise we don't receive.
			Application.runInBackground = true;

			// Setup network

			// use Debug.Log functions for Telepathy so we can see it in the console
			Telepathy.Log.Info = Debug.Log;
			Telepathy.Log.Warning = Debug.LogWarning;
			Telepathy.Log.Error = Debug.LogError;

			_client = new Telepathy.Client(65535);

			// hook up events
			_client.OnConnected = OnConnect;
			_client.OnData = OnDataReceived;
			_client.OnDisconnected = OnDisconnect;
		}

		// Update is called once per frame
		void Update()
		{
			// tick to process messages
			// (even if not connected so we still process disconnect messages)
			_client.Tick(100);
		}

		void OnApplicationQuit()
		{
			_client.Disconnect();
		}

		void OnButtonConnectClick()
		{
			_client.Connect(ServerHost, ServerPort);
		}

		void OnInputFieldEndEdit(string new_value)
		{
			SendString(new_value);
			inputFieldChatText.text = "";

			StartCoroutine(SelectInputField());
		}

		// ====================================================================
		// Network callbacks
		// ====================================================================

		void OnConnect()
		{
			log.Info($"Connected to server");

			SendConnectMessage(colorPicker.color, inputFieldName.text);

			panelLogin.gameObject.SetActive(false);

			scrollView.gameObject.SetActive(true);

			inputFieldChatText.gameObject.SetActive(true);
			StartCoroutine(SelectInputField());
		}

		void OnDataReceived(ArraySegment<byte> data)
		{
			log.Trace($"data received. Len: {data.Count} bytes. '{BitConverter.ToString(data.Array, data.Offset, data.Count)}'");

			string message = System.Text.Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);

			log.Trace(message);

			textScrollViewContent.text += $"{((textScrollViewContent.text.Length > 0) ? Environment.NewLine : "")}{message}";

			Canvas.ForceUpdateCanvases();

			scrollView.verticalNormalizedPosition = 0; // scroll to bottom
		}

		void OnDisconnect()
		{
			log.Info($"disconnected");
		}

		void SendConnectMessage(Color color, string username)
		{
			SendString($"{ColorUtility.ToHtmlStringRGB(color)}{username}");
		}

		void SendChatMessage(string text)
		{
			SendString(text);
		}

		void SendString(string s)
		{
			log.Debug("SendString({0})", s);
			_client.Send(new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(s)));
		}

		IEnumerator SelectInputField()
		{
			yield return new WaitForEndOfFrame();
			inputFieldChatText.ActivateInputField();
		}

		static readonly LibCSharp.Logger log = LibCSharp.Logger.GetCurrentClassLogger();
	}
}

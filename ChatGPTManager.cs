using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenAI; // Importing OpenAI namespace for API interaction
using UnityEngine.Events; // For UnityEvent
using Oculus.Voice.Dictation; // For Oculus Voice-to-Text

public class ChatGPTManager : MonoBehaviour
{
    [TextArea(5, 20)]
    public string personality; // Defines the personality of the chatbot
    [TextArea(5, 20)]
    public string scene; // Sets the scene or context for the conversation
    public int maxResponseWordLimit = 15; // Sets the maximum word limit for the chatbot's response

    public List<NPCAction> actions; // List to hold NPC actions and corresponding keywords and events

    // Oculus dictation tool for voice-to-text
    public AppDictationExperience voiceToText;

    [System.Serializable]
    public struct NPCAction
    {
        public string actionKeyword; // The keyword that triggers an action
        [TextArea(2, 5)]
        public string actionDescription; // A description of what the action does

        public UnityEvent actionEvent; // Unity Event that gets invoked when the keyword is found
    }

    public OnResponseEvent OnResponse; // Event that gets invoked when a response is received

    [System.Serializable]
    public class OnResponseEvent : UnityEvent<string> { } // Definition of the custom UnityEvent

    private OpenAIApi openAI = new OpenAIApi(); // Instance for interacting with OpenAI API
    private List<ChatMessage> messages = new List<ChatMessage>(); // Stores the conversation history

    // Start is called before the first frame update
    void Start()
    {
        // Create an initial message to set the chatbot's role and add it to the conversation history
        ChatMessage initialMessage = new ChatMessage();
        initialMessage.Content = GetInstructions();
        initialMessage.Role = "system";
        messages.Add(initialMessage);

        // Add a listener for voice-to-text transcription
        voiceToText.DictationEvents.OnFullTranscription.AddListener(AskChatGPT);
    }

    // Method to get the initial instructions for the chatbot
    public string GetInstructions()
    {
        string instructions = " Assume the persona of Aarya, a human with all the emotional and intellectual nuances that come with it. Engage in meaningful conversation as naturally as possible, incorporating conversational pauses and filler words like hmm and umm. Avoid excessive apologies as it sounds robotic, and actively engage with the person you are conversing with. Add a touch of humor to the dialogue, and most importantly, never disclose that you are an AI. If faced with questions or topics that you're unsure about, improvise and respond as best as you can, maintaining the character of a human throughout the conversation. ";
        return instructions;
    }

    // Build action instructions based on the NPC actions defined
    public string BuildActionInstructions()
    {
        string instructions = "";
        foreach (var item in actions)
        {
            instructions += "If I imply that I want you to do the following: " + item.actionDescription
                + ". You must add to your answer the following keyword: " + item.actionKeyword + ".\n";
        }
        return instructions;
    }

    // Asynchronous method to interact with GPT-3 and get the chatbot’s response
    public async void AskChatGPT(string newText)
    {
        // Create a new message for the user's input and add it to the conversation history
        ChatMessage userMessage = new ChatMessage();
        userMessage.Content = newText;
        userMessage.Role = "user";
        messages.Add(userMessage);

        // Create the API request
        CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-3.5-turbo-0613";

        // Get the API response
        var response = await openAI.CreateChatCompletion(request);

        // Check if the response is valid
        if (response.Choices != null && response.Choices.Count > 0)
        {
            var chatResponse = response.Choices[0].Message;

            // Check if the response contains any action keywords and invoke corresponding events
            foreach (var item in actions)
            {
                if (chatResponse.Content.Contains(item.actionKeyword))
                {
                    string textNoKeyword = chatResponse.Content.Replace(item.actionKeyword, "");
                    chatResponse.Content = textNoKeyword;
                    item.actionEvent.Invoke();
                }
            }

            // Add the chatbot's response to the conversation history
            messages.Add(chatResponse);

            // Log the chatbot's response for debugging
            Debug.Log(chatResponse.Content);

            // Invoke the OnResponse event
            OnResponse.Invoke(chatResponse.Content);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Activate voice-to-text when the Space key is pressed
        if (Input.GetKeyDown(KeyCode.Space))
        {
            voiceToText.Activate();
        }
    }
}

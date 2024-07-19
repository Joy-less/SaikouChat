<img src="https://github.com/Joy-less/SaikouChat/blob/b753651a17ff8ddee32c2cd9a1a34a0eb35fb409/Images/IconGray.png" width="150" />

# Saikou Chat
 
Saikou Chat is an application to chat with characters using AI.

It uses local models for a free and open-source experience.

## Features

- Create and chat with AI characters
- Runs offline, for free, and is open-source
- All character data stored in a JSON file for portability and editability
- Regenerate messages you don't like
- Bring your own `.gguf` model

## To-do

- Pin messages
- Group chats

## Disclaimers

- Responses can take a while to generate
- Responses may be disappointing based on the model
- Models are usually several gigabytes in size
- Only one response can be generated at a time (other responses are deferred)

## Tutorial

1. Download the latest version of Saikou Chat.

2. You need a large language model compatible with [llama.cpp](https://github.com/ggerganov/llama.cpp) to generate responses.

Here are some free models:
- [Dolphin 2.9 Llama 3](https://huggingface.co/QuantFactory/dolphin-2.9-llama3-8b-GGUF/tree/main) (recommended: `dolphin-2.9-llama3-8b.Q5_K_M`)
- [Meta Llama 3](https://huggingface.co/QuantFactory/Meta-Llama-3-8B-GGUF/tree/main)

3. Run Saikou Chat, open the settings and set the `Model Path` to your model.

4. Create a character and start chatting!

## Tips

- You can find detailed anime/manga character biographies on [MyAnimeList](https://myanimelist.net/character.php)
- To regenerate a message, press the Regenerate button to delete it and try again (warning: also deletes all subsequent messages)
- To delete a message, press the Delete button
- To generate a message without sending one, press the Generate button
- To describe the setting of your conversation, press the Edit Scene Description button
<img src="https://github.com/Joy-less/SaikouChat/blob/b753651a17ff8ddee32c2cd9a1a34a0eb35fb409/Images/IconGray.png" width="150" />

# Saikou Chat
 
Saikou Chat is an application to chat with characters using AI.

It uses local models for a free and open-source experience.

## Features

- Create and chat with AI characters
- Group chats with multiple characters
- Free & open-source and uses your own local large language model
- Character data stored in a JSON file for portability and editability
- Pin messages you like
- Regenerate or delete messages you don't like

## Screenshots

<img src="https://github.com/Joy-less/SaikouChat/blob/db3658d630dd5fe7d0b6ce5f1a93ab12900fa9f3/Images/Screenshots/Screenshot1.png" width="300" /> <img src="https://github.com/Joy-less/SaikouChat/blob/db3658d630dd5fe7d0b6ce5f1a93ab12900fa9f3/Images/Screenshots/Screenshot2.png" width="300" />

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
- You can describe a setting for the chat with the Edit Scene Description button. The setting is always included in the prompt
- You can hold shift to select multiple characters when creating a chat
- You can pin messages with the Pin button. Pinned messages are always included in the prompt, even if they exceed the message history limit
- You can generate messages manually with the Generate button
- You can regenerate bad messages with the Regenerate button. The message and all subsequent messages will be deleted and a new message generated
- You can delete a message with the Delete button
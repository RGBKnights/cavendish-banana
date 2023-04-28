using AngleSharp;
using AngleSharp.Dom;
using CavendishBanana.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using ReverseMarkdown;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static OpenAI.GPT3.ObjectModels.StaticValues;
using Models = OpenAI.GPT3.ObjectModels.Models;

namespace CavendishBanana
{
    public class BananasFunction
    {
        private readonly IOpenAIService openAiService;
        private readonly Converter mdConverter;

        public BananasFunction(IOpenAIService openAiService, Converter mdConverter)
        {
            this.openAiService = openAiService;
            this.mdConverter = mdConverter;
        }

        [FunctionName("Test")]
        public async Task<IActionResult> Test(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "test")] ChatCompletionRequestDTO body,
           ILogger logger)
        {
            var json = "{\"message\":{\"role\":\"assistant\",\"content\":\"Once upon a time, in a kingdom far, far away, there was a young princess named Amelia. She was a beautiful and gentle soul, with a heart full of kindness and courage. Amelia loved to go on adventures, exploring the world around her, much to the dismay of her family and the Royal Guard.  One sunny day, Princess Amelia ventured into the woods on her trusty horse, Whistler. She was accompanied by her loyal friend, a small fox named Rusty, who knew the forest as well as he knew his own tail. Riding along the forest trails with Rusty by her side, Amelia felt alive and at peace, away from the hustle and bustle of the kingdom.  As they continued on their journey, the sun began to dip below the horizon, casting shadows upon the ground. Soon, the narrow paths became increasingly tricky to navigate, and Amelia found herself deeper in the forest than she had ever been before. Despite Rusty's keen sense of direction, the usual twists and turns they took now felt unfamiliar and unsettling.  Realizing they were lost, panic took hold of Amelia's heart, her mind racing with thoughts of the darkness and the unknown. Whistler sensed her distress and neighed nervously, while Rusty's ears perked up, alert to any potential danger lurking in the shadows.  As the darkness enveloped the woods, Amelia took a deep breath, hoping to muster enough courage to find her way out. By sheer chance or perhaps a stroke of luck, they stumbled upon a small, glimmering light in the distance. With no other choice but to press forward, the trio cautiously approached the mysterious light.  As the light grew brighter, the forest came alive with enchanting creatures and glowing plants, a scene that took Amelia's breath away. It was as if they had walked into a different world altogether, where magic resided and the impossible became possible.  In the center of this otherworldly gathering stood an ancient oak tree, radiating its own ethereal light. Mesmerized, Amelia approached the tree, and etched on its trunk was an inscription that read:  \\\"Though lost in dark, don't despair, Believe in yourself and a brave heart flare, In the right direction, we will lead, The strength you need; you already breed.\\\"  Reading these words aloud, a newfound strength swelled within Amelia. She realized that it was not the light that could guide her home, but her own courage and determination. With renewed vigor, she mounted Whistler and whispered into his ear, \\\"Take us home, my noble friend.\\\"  Guided by Amelia's unwavering resolve and Rusty's uncanny instincts, they retraced their steps and emerged from the woods just as the moon settled high in the sky. Home stood in the distance, illuminated under the pale moonlight.  The kingdom rejoiced at the safe return of their beloved princess, and Amelia became a legend for her bravery and wit. From that day on, she approached life with a fearless and adventurous spirit, never forgetting the magical night that taught her the true meaning of courage.  And so, Princess Amelia, Rusty, and Whistler went on countless adventures, their bond stronger than ever. And in the kingdom far, far away, they lived happily ever after.\"}}";
            var dto = JsonConvert.DeserializeObject<ChatCompletionResponseDTO>(json);
            return new JsonResult(dto);
        }

        [FunctionName("Chat")]
        public async Task<IActionResult> Chat(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] ChatCompletionRequestDTO body,
            ILogger logger)
        {

            try
            {
                if (body.User is null)
                    throw new ArgumentException("Invalid User");
                if (body.Messages is null)
                    throw new ArgumentException("Invalid Chat Messages");

                var messages = body.Messages.Select(_ => new ChatMessage(_.Role, _.Content)).ToList();

                var content = new StringBuilder();
                var req = new ChatCompletionCreateRequest
                {
                    User = body.User,
                    Messages = messages,
                    Model = Models.Gpt_4,
                    MaxTokens = 4000
                };

                var watcher = new Stopwatch();
                watcher.Start();
                logger.LogInformation("Chat Request");
                var stream = openAiService.ChatCompletion.CreateCompletionAsStream(req);
                await foreach (var completionResult in stream)
                {
                    var response = completionResult.Choices.FirstOrDefault()?.Message?.Content ?? string.Empty;
                    content.Append(response);
                }
                watcher.Stop();
                logger.LogInformation($"Chat Stream Complete after {watcher.Elapsed}");

                var md = this.mdConverter.Convert(content.ToString());

                var result = new ChatCompletionResponseDTO()
                {
                    Message = new ChatMessageDTO() { Role = ChatMessageRoles.Assistant, Content = md }
                };

                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new BadRequestResult();
            }
        }

        [FunctionName("Whisper")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public async Task<IActionResult> Whisper(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "whisper")] HttpRequest req,
            ILogger logger)
        {
            try
            {
                var file = req.Form.Files.FirstOrDefault();

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var data = ms.ToArray();

                var transcriptionResult = await openAiService.Audio.CreateTranscription(new AudioCreateTranscriptionRequest()
                {
                    FileName = "recording.webm",
                    File = data,
                    Model = Models.WhisperV1,
                    Language = "en",
                    ResponseFormat = "text",
                });

                if (!transcriptionResult.Successful)
                    throw new InvalidOperationException(transcriptionResult.Error?.Message ?? "Failed Transcription");

                var result = new TranscriptionResponseDTO()
                {
                    Transcription = transcriptionResult.Text
                };
                return new JsonResult(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new BadRequestResult();
            }
        }

        [FunctionName("Fetch")]
        public async Task<IActionResult> Fetch(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fetch")] FetchRequestDTO body,
           ILogger logger)
        {
            try
            {
                var config = Configuration.Default.WithDefaultLoader();
                var address = body.Url ?? throw new ArgumentNullException("Invalid Url");
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(address);
                var md = this.mdConverter.Convert(document.TextContent);
                return new JsonResult(md);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new BadRequestResult();
            }
        }
    }
}

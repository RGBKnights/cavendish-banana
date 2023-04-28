using AngleSharp;
using CavendishBanana.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels.RequestModels;
using ReverseMarkdown;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

                var result = new ChatCompletionResponseDTO()
                {
                    Message = new ChatMessageDTO() { Role = ChatMessageRoles.Assistant, Content = content.ToString() }
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

using Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Azure.AI.Language.QuestionAnswering;


namespace Labb1_nlp
{
	internal class Program
	{

		private static string Uri;
		private static string qnaSvcKey;
		private static string cogSvcKey;
		private static string cogSvcRegion;

		private static SpeechConfig speechConfig;

		static async Task Main(string[] args)
		{
			string question = "";
			
			string projectName = "FaqProject";
			string deploymentName = "production";
			//Read json
			IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
			IConfigurationRoot configuration = builder.Build();
			Uri = configuration["ServiceQnaEndpoint"];
			qnaSvcKey = configuration["ServiceQnaKey"];
			cogSvcKey = configuration["CognitiveServiceKey"];
			cogSvcRegion = configuration["CognitiveServiceRegion"];

			speechConfig = SpeechConfig.FromSubscription(cogSvcKey, cogSvcRegion);
			speechConfig.SpeechSynthesisVoiceName = "en-US-AriaNeural";
			AzureKeyCredential qnaCredentials = new AzureKeyCredential(qnaSvcKey);

			Uri qnaEndpoint = new Uri(Uri);
			QuestionAnsweringClient qnaClient = new QuestionAnsweringClient(qnaEndpoint, qnaCredentials);
			QuestionAnsweringProject project = new QuestionAnsweringProject(projectName, deploymentName);

			
			//Console.InputEncoding = Encoding.Unicode;
			//Console.OutputEncoding = Encoding.Unicode;

			while (question.ToLower() != "quit")
			{
				Console.WriteLine("Ask me something");

				//question = Console.ReadLine();
				question = await TranscribeQuestionFromMic(question);
				try
				{
					Response<AnswersResult> response = qnaClient.GetAnswers(question, project);
					foreach (KnowledgeBaseAnswer answer in response.Value.Answers)
					{
						Console.WriteLine($"Q:{question}");
						await TranscribeAnswer(answer.Answer);
						
					}
				}
				catch (Exception ex)
				{
					
				}
			}
		}
		static async Task<string> TranscribeQuestionFromMic(string question)
		{
			string micQuestion = "";
			using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
			using SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);

			var result = await speechRecognizer.RecognizeOnceAsync();
			micQuestion = result.Text;
			return micQuestion;
		}
		static async Task TranscribeAnswer(string answer)
		{
			Console.WriteLine(answer);
			string responseText = answer;
			using AudioConfig audioConfig = AudioConfig.FromDefaultMicrophoneInput();
			using SpeechRecognizer speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
			using SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(speechConfig);
			string responseSsml = $@"
				<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
					<voice name='en-GB-LibbyNeural'>
						{responseText}
						<break strength='weak'/>
					</voice>
				</speak>";
			SpeechSynthesisResult speak = await speechSynthesizer.SpeakSsmlAsync(responseSsml);
			var result = await speechRecognizer.RecognizeOnceAsync();
			if (speak.Reason != ResultReason.SynthesizingAudioCompleted)
			{
				Console.WriteLine(speak.Reason);
			}
		}
	}
}
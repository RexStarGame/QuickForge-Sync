using System;
using OpenAI.Chat;
using System.ClientModel;

namespace exam_test
{
    public static class OpenAITest
    {
        public static string AskTest()
        {
            try
            {
                string? key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

                if (string.IsNullOrWhiteSpace(key))
                {
                    key = Environment.GetEnvironmentVariable(
                        "OPENAI_API_KEY",
                        EnvironmentVariableTarget.User
                    );
                }

                if (string.IsNullOrWhiteSpace(key))
                {
                    return "OPENAI_API_KEY blev ikke fundet.";
                }

                ChatClient client = new ChatClient(
                    model: "gpt-5.1",
                    apiKey: key
                );

                ChatCompletion completion = client.CompleteChat(
                    "Forklar recursion i C# med én kort sætning."
                );

                return completion.Content[0].Text;
            }
            catch (ClientResultException ex)
            {
                return "OpenAI API fejl: " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Ukendt fejl: " + ex.Message;
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.RegularExpressions;
using selfproj.Models;
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace selfproj.Controllers
{
    [ApiController]
    [Route("home")]
    public class HomeController : Controller
    {
        private IConfiguration configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public HomeController(IConfiguration localconfig,
            IWebHostEnvironment webHostEnvironment)
        {
            configuration = localconfig;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpPost("askme")]
        public async Task<JsonResult> askmeany([FromBody] string q)
        {

            q = Regex.Replace(q, @"[^a-zA-Z0-9\s]", "").ToLower();
            if (q.Replace(" ", "").ToLower().IndexOf("whatis") == -1)
            {
                q = "what is " + q;
            }

            var chatResponse = await chathelper.connect_and_get("", q, configuration);
            List<openapireturn> indentified_props = JsonConvert.DeserializeObject<List<openapireturn>>(chatResponse);
            indentified_props = indentified_props.DistinctBy(x => x.subject).ToList();
            List<answer_return> allanswers = new List<answer_return>();
            string translation_comm = " Translate the user query to " + indentified_props[0].detectedlanguage.ToString() + " language, if the user query is already in " +
                indentified_props[0].detectedlanguage.ToString() + ", then send it back, as it is \"";
            string simplification_systemcommand = "Simplify this JSON into human readable text, combining name, " +
                "value, and unit into simple phrases like: 'width is 10mm'. Ignore symbols and null values. " +
                "If not found return \"There are no attributes to display.\"" +
                "If the JSON is empty return \"There are no attributes to display.\"" +
                "If the JSON is not readble or invalid return \"There are no attributes to display. \"";
            string simplification_systemcommand_josn = "Simplify this JSON into human readable text, " +
                "If there's a link, Send the link Directly, Do not add extra explanation";

            foreach (openapireturn rn in indentified_props)
            {
                answer_return single_answer = new answer_return();
                single_answer.subject = rn.subject;
                single_answer.attribute = rn.attribute;
                single_answer.comparative = rn.comparative;
                single_answer.detectedlanguage = rn.detectedlanguage;
                List<string> retstring = new List<string>();
                List<string> retstring_translated = new List<string>();
                try
                {
                    if (string.IsNullOrEmpty(rn.attribute))
                    {
                        rn.attribute = "shortdescription";
                    }
                    else if (rn.attribute.ToLower().IndexOf("difference") > -1) { rn.attribute = "description"; }

                    rn.attribute = Regex.Replace(rn.attribute, @"[^a-zA-Z0-9\s]", "");
                    rn.subject = rn.subject.Replace(" ", "");
                    string jsonfile = _webHostEnvironment.ContentRootPath + "\\dummydata\\" + rn.subject.Replace(" ", "_") + ".json";
                    StreamReader sr = new StreamReader(jsonfile);
                    string raw_json = "";
                    while (true)
                    {
                        string line = sr.ReadLine();
                        if (line == null)
                        {
                            break;
                        }
                        raw_json = raw_json + line;
                    }
                    sr.Close();

                    BsonDocument bsonDocument = BsonSerializer.Deserialize<BsonDocument>(raw_json);
                    List<string> allkeys_ = bsonDocument.Names.ToList();
                    foreach (string key in allkeys_)
                    {
                        if (rn.attribute.ToLower().IndexOf(key) != -1 || key.ToLower().IndexOf(rn.attribute.ToLower()) != -1)
                        {
                            string currentdata_ = bsonDocument.GetValue(key).ToString();
                            try
                            {
                                BsonSerializer.Deserialize<BsonArray>(currentdata_);
                                retstring.Add(await chathelper.connect_and_get(simplification_systemcommand, currentdata_, configuration));
                            }
                            catch (Exception e)
                            {
                                retstring.Add(bsonDocument.GetValue(key).ToString());
                            }
                        }
                    }
                    BsonArray br_all = new BsonArray();
                    List<string> inner_searchKeys = configuration["inner_params"].ToString().Split(",").ToList();
                    string inner_searchKey = configuration["inner_params_compare"].ToString();

                    if (retstring.Count == 0)
                    {
                        foreach (string key in allkeys_)
                        {
                            try
                            {
                                BsonArray bar = BsonSerializer.Deserialize<BsonArray>(bsonDocument.GetValue(key).ToString());
                                foreach (BsonDocument bs in bar)
                                {

                                    string str_innerelement_retified = Regex.Replace(bs.GetValue(inner_searchKey).ToString(), @"[^a-zA-Z0-9\s]", "");
                                    if (rn.attribute.ToLower().IndexOf(str_innerelement_retified) != -1 || str_innerelement_retified.ToLower().IndexOf(rn.attribute.ToLower()) != -1)
                                    {
                                        retstring.Add(await chathelper.connect_and_get(simplification_systemcommand,
                                        bs.ToJson(), configuration));
                                    }
                                    br_all.Add(new BsonDocument(bs));
                                }

                            }
                            catch (Exception e)
                            {
                            }
                        }

                    }
                    if (retstring.Count == 0)
                    {
                        string simplification_systemcommand_2 = "You will receive a JSON array of \"\"attribute\"\" objects," +
                                    "where each object includes fields such as \"\"" + string.Join("\"\",\"\"", inner_searchKeys) + "\"\". " +
                                    "where field value is \"" + rn.attribute + "\", " +
                                    "match (match will be case-insensitive, partial matches allowed, similarity matches allowed)" +
                                    " it to the \"\"" + inner_searchKey + "\"\" field. " +
                                    "Once found, " +
                                    "return the \"\"attribute\"\"" +
                                    "If no match is found, return: \"No matching attribute found.\"" +
                                    "Do not add any explanation, outside of the JSON";
                        string retstring_ = await chathelper.connect_and_get(simplification_systemcommand_2,
                                        br_all.ToJson().ToString(), configuration);
                        if (retstring_.ToLower().Replace(" ", "").IndexOf("nomatchingattributefound") == -1)
                        {
                            retstring.Add(await chathelper.connect_and_get(simplification_systemcommand_josn, retstring_, configuration));
                        }

                    }
                    if (retstring.Count == 0)
                    {

                        string simplification_systemcommand_2 = "You will receive a JSON input " +
                            "and the user question is : \"" + rn.attribute + " of " + rn.subject + "\". " +
                            "First, convert the JSON into a simple, " +
                            "human-readable text format. Then, answer the user's " +
                            "question strictly based on that text. Do not include any " +
                            "explanations, assumptions, or additional commentary, " +
                            "keep the as short as possible and " +
                            "to the point " +
                            "and only retrun the answer of user's question, no other details";

                        retstring.Add(await chathelper.connect_and_get(simplification_systemcommand_2, raw_json, configuration));

                    }
                    foreach (string str in retstring)
                    {
                        retstring_translated.Add(await chathelper.connect_and_get(translation_comm, str, configuration));
                    }
                }
                catch (Exception ex)
                {

                    retstring_translated.Add("Opps, " +
                        "Can you pleas refrese the question, " +
                        "You can ask in humanly manner, " +
                        "Use adverb like Of or For, " +
                        "I can only answer from my existing knowledge-base");
                }
                single_answer.answer = retstring_translated;
                allanswers.Add(single_answer);
            }
            if (allanswers.Count() > 1 && allanswers[0].comparative)
            {
                List<string> companswer = new List<string>();
                answer_return compareanswer = new answer_return();
                List<string> allanswers_ = new List<string>();
                string simplification_systemcommand_compare = "You will receive a JSON list of strings. " +
                                        "Your task is to compare all the strings find out the quantitative(Number)" +
                                        " difference and merge them into one concise and meaningful statement " +
                                        "that captures the common idea or summarizes the main point of the list," +
                                        "If not quantitative(Number) differnce found, then just add \"No difference found\", at the and of the answer" +
                                        "If differnce found, then just add \"The difference is\", and the add the quantitative difference " +
                                        "Do not include any " +
                                        "explanations, assumptions, or additional commentary, " +
                                        "keep the as short as possible and " +
                                        "to the point " +
                                        "and only retrun the answer of user's question, no other details";
                foreach (answer_return oans in allanswers)
                {
                    oans.comparative = false;
                    compareanswer.subject = compareanswer.subject + oans.subject + ",";
                    compareanswer.attribute = compareanswer.attribute + oans.attribute + ",";
                    allanswers_.Add("for the subject \"" + oans.subject + "\" " + string.Join(" ", oans.answer));
                }
                compareanswer.subject = (compareanswer.subject + ",,").Replace(",,,", "");
                compareanswer.attribute = (compareanswer.attribute + ",,").Replace(",,,", "");
                companswer.Add(await chathelper.connect_and_get(simplification_systemcommand_compare,
                    JsonConvert.SerializeObject(allanswers_).ToString(), configuration));
                compareanswer.answer = companswer;
                compareanswer.comparative = true;
                if (compareanswer.answer[0].ToLower().Replace(" ", "") != "thedifferenceis1.")
                {
                    allanswers.Add(compareanswer);
                }
                return Json(allanswers);
            }
            else
            {
                allanswers[0].comparative = false;
                return Json(allanswers);
            }

        }


    }
    public class chathelper
    {
        public static async Task<string> connect_and_get(string system_query, string user_query, IConfiguration config_)
        {
            try
            {
                if (string.IsNullOrEmpty(system_query))
                {
                    system_query = "You are an intelligent data extractor." +
                    "Your task is to analyze a user's natural language question and identify:\n" +

                    "- The main subject-ID, which starts with one or more digits (0-9) without any spaces in between. " +
                    "The subject-ID may optionally end with a single lowercase or Uppercase letter (a-z, A-Z) preceded by a space " +
                    "(e.g., " + config_["subject_identification_param"].ToString() + ",etc)\n" +

                    "- The " +
                    string.Join(" or ",config_["attribute"].ToString())+
                    " the user is asking about " +
                    "(e.g., " + config_["attribute_identification_param"].ToString() + ", etc)\n" +

                    "Output format must be in valid JSON,list of objects:\n" +
                    "[{\n" +
                      "\"\"subject\"\": \"\"<main subject or entity being referred to>\"\",\n" +
                      "\"\"attribute\"\": \"\"<what the user wants to know about the subject>\"\"\n" +
                    "}]\n" +

                    "Do not add any explanation or extra text outside of the JSON and please do spelling check,If the \"attribute\" " +
                    "is in other languages, Translate it to English " +
                    "Ignore the symbols but treat \",\" as \"and\" " +
                    "If the user is trying compare or find out difference between subjects, then " +
                    "add another parameter called \"\"comparative\"\" as \"true\", " +
                    "If the user is NOT trying compare or find out difference between subjects, Just quering, then" +
                    "add another parameter called \"\"comparative\"\" as \"false\"" +
                    "add another parameter called \"\"detectedlanguage\"\" and add the value of the user query language";
                }
                string endpoint = config_["endpoint"].ToString();
                string apiKey = config_["apiKey"].ToString();
                string deploymentName = config_["deploymentName"].ToString();
                ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);
                AzureOpenAIClient client = new AzureOpenAIClient(new Uri(endpoint), apiKeyCredential);
                ChatClient chatClient = client.GetChatClient(deploymentName);
                List<ChatMessage> messages = new List<ChatMessage>();
                messages.Add(new SystemChatMessage(system_query));
                messages.Add(new UserChatMessage(user_query));
                var response = await chatClient.CompleteChatAsync(messages, new ChatCompletionOptions()
                {
                    Temperature = (float)0.7
                });
                var chatResponse = response.Value.Content.Last().Text;
                return chatResponse;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

       
    }
}

using APIMocker;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
List<APIDetailOptions> APIDetailOptions = new List<APIDetailOptions>();


// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<APIDetailOptions>(
          builder.Configuration.GetSection("APIDetail"));

builder.Configuration.GetSection("APIDetail").Bind(APIDetailOptions);


var app = builder.Build();

app.UseHttpsRedirection();
app.UseStatusCodePages(async context =>
{
    var request = context.HttpContext.Request;
    var path = request.Path.Value;
    var queryString = request.QueryString.Value;
    var method = request.Method;
    var response = context.HttpContext.Response;
    var match = APIDetailOptions.FirstOrDefault(opt =>
              path.Equals(opt.Path, StringComparison.OrdinalIgnoreCase) &&
              QueryStringChecker.Match(opt.QueryString, queryString) &&
              string.Equals(opt.Method, method, StringComparison.InvariantCultureIgnoreCase));
    if (match != null)
    {
        await Respond(response, match);
    }
});


app.MapGet("/", async context =>
{
    await WriteWelcomePage(context.Response, context.Request);
});

app.Run();

async Task WriteWelcomePage(HttpResponse response, HttpRequest request)
{
    response.ContentType = "text/html";
    var sb = new StringBuilder($"<span style='display: inline-flex;align-items: baseline;'><h2>MockApi</h2>--" +
        $"<h4>{MethodBase.GetCurrentMethod().DeclaringType.Assembly.GetName().Version}</h4>" +
        $"</span>");
    var url = $"{(request.IsHttps ? "https" : "http")}://{request.Host}";
    sb.Append($@"<script>
      function callPost(path, queryString, method) {{
        var xhr = new XMLHttpRequest();
        xhr.onreadystatechange = function() {{
          if (this.readyState == 4 && this.status == 200) {{
              alert(xhr.responseText);
            }}
        }};
        var yourUrl='{url}' + path + queryString;
        xhr.open(method, yourUrl, true);
        xhr.setRequestHeader('Content-Type', 'application/json');
        xhr.send();
      }}
</script>");
    sb.Append("<p style='color: green'>running</p>");
    sb.Append("<br><p>registered API mocks:</p>");
    sb.Append("<style>table {border-collapse: collapse;} td, th { border: 1px solid black; }</style>");
    sb.Append("<table><tr><th>Path</th><th>Query string</th><th>method</th><th></th></tr>");
    APIDetailOptions.ForEach(opt =>
    {
        sb.Append($"<tr><td>{opt.Path}</td><td>{opt.QueryString}</td><td>{opt.Method.ToUpper()}</td><td>");
        if (opt.Method == "GET")
        {
            sb.Append($"<a target='_blank' href='{url}{opt.Path}{opt.QueryString}'>test</a>");
        }
        else
        {
            sb.Append($"<button onclick='callPost(\"{opt.Path}\",\"{opt.QueryString}\",\"{opt.Method}\")'>test</button>");
        }
        sb.Append("</td></tr>");
    });
    sb.Append("</table>");
    await response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(sb.ToString()));
}

async Task Respond(HttpResponse response, APIDetailOptions match)
{
    response.StatusCode = match.StatusCode;
    var stream = response.Body;
    if (match.ResponseHeaders != null && response.Headers != null)
    {
        match.ResponseHeaders.Where(rh => rh.Key != null && rh.Value != null).ToList().ForEach(resHeader =>
          response.Headers.Add(resHeader.Key, resHeader.Value));
    }

    var responseBodyString = !string.IsNullOrWhiteSpace(match.ResponseBody) ?
      match.ResponseBody :
      File.ReadAllText(match.ResponseBodyFile);

    if (match.Replaces.Any())
    {
        responseBodyString = ApiMocker.Replace(responseBodyString, match.Replaces);
    }
    await stream.WriteAsync(Encoding.UTF8.GetBytes(responseBodyString));
}

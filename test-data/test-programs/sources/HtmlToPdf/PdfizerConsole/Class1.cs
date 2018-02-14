using System;
using System.IO;
using System.Text;

namespace PdfizerConsole
{
	using iTextSharp.text;
	using Pdfizer;

	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
            String success = "SUCCESS";
			StringWriter sb = new StringWriter();
			sb.WriteLine("<html>");
			sb.WriteLine("<head>");
			sb.WriteLine("</head>");
			sb.WriteLine("<body>");

			sb.WriteLine("<h1>h1 title</h1>");
			sb.WriteLine("<h2>h2 title</h2>");
			sb.WriteLine("<h3>h3 title</h3>");
			sb.WriteLine("<h3>h3(2) title</h3>");
			sb.WriteLine("<h4>h4 title</h4>");
			sb.WriteLine("<h4>h4(2) title</h4>");
			sb.WriteLine("<h5>h5 title</h5>");
			sb.WriteLine("<h5>h5(2) title</h5>");
			sb.WriteLine("<h6>h6 title</h6>");
			sb.WriteLine("<h6>h6(2) title</h6>");
			sb.WriteLine("<h6>h6(3) title</h6>");
			sb.WriteLine("<h2>h2(2) title</h2>");
			sb.WriteLine("<h3>h3 title</h3>");
			sb.WriteLine("<h4>h4 title</h4>");
			sb.WriteLine("<h5>h5 title</h5>");
			sb.WriteLine("<h6>h6 title</h6>");
	
			sb.WriteLine("<p>p paragraph</p>");
			sb.WriteLine("<pre>pre paragraph</pre>");
			sb.WriteLine("<div class=\"b\">div paragraph (class=b)</div>");
			sb.WriteLine("<p>a link: <a href=\"http://www.foo.com\">hyperlink</a></p>");
			
			sb.WriteLine("<p align=\"left\">left aligned</p>");
			sb.WriteLine("<p align=\"right\">right aligned</p>");
			sb.WriteLine("<p align=\"center\">centered</p>");

			sb.WriteLine("<p>paragraph with <b>bold (b)</b> elements</p>");
			sb.WriteLine("<p>paragraph with <em>emphasized (em)</em> elements</p>");
			sb.WriteLine("<p>paragraph with <tt>typewriter (tt)</tt> elements</p>");
			sb.WriteLine("<p>paragraph with <code>code (code)</code> elements</p>");
			sb.WriteLine("<p>paragraph with <span class=\"em\">span elements (class = em)</span></p>");
		
			sb.WriteLine("<ul><li>Unordered</li><li>list</li></ul>");
			sb.WriteLine("<ol><li>Ordered</li><li>list</li></ol>");
		
			sb.WriteLine("<ol>");
			sb.WriteLine("<li>");
			sb.WriteLine("<ul><li>sub-item 1</li><li>sub item 2</li></ul>");
			sb.WriteLine("</li>");
			sb.WriteLine("<li>ordered item</li></ol>");

			sb.WriteLine("<p align=\"center\"><img src=\"image.png\" height=\"400\" width=\"400\" /></p>");

			sb.WriteLine("</body>");
			sb.WriteLine("</html>");
		
			Console.WriteLine(sb.ToString());
		
			HtmlToPdfConverter	html2pdf = new HtmlToPdfConverter();

			try
			{
                String outputFilename = "output.pdf";
                File.Delete(outputFilename);
                FileStream outputWriter = new FileStream(outputFilename, FileMode.OpenOrCreate);
                html2pdf.Open(outputWriter);
				html2pdf.AddChapter(@"Dummy Chapter");
				html2pdf.Run(sb.ToString());
				html2pdf.AddChapter(@"A Wiki page");
				html2pdf.ImageUrl = true;

				TextReader reader = new StreamReader("input.htm");
				
				html2pdf.Run(reader.ReadToEnd());

				html2pdf.AddChapter(@"Boost page");
				html2pdf.Close();
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.ToString());
                success = "FAIL";
			}
            Console.WriteLine(success);
		}
	}
}

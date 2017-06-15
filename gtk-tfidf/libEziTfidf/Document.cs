//l.kasprzyk@gmail.com
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libEziTfidf
{
    public class Document
    {
        private static string stopwordssrt = "a,able,about,across,after,all,almost,also,am,among,an,and,any,are,as,at,be,because,been,but,by,can,cannot,could,dear,did,do,does,either,else,ever,every,for,from,get,got,had,has,have,he,her,hers,him,his,how,however,i,if,in,into,is,it,its,just,least,let,like,likely,may,me,might,most,must,my,neither,no,nor,not,of,off,often,on,only,or,other,our,own,rather,said,say,says,she,should,since,so,some,than,that,the,their,them,then,there,these,they,this,tis,to,too,twas,us,wants,was,we,were,what,when,where,which,while,who,whom,why,will,with,would,yet,you,your";        
		private static string[] keywords = null;
		private static string[] orgkeywords = null;
        public static string[] Keywords
        {
            get { return Document.keywords; }
			set { Document.keywords = value; }
        }
		
		public static string[] KeywordsOrg
		{
			get { return orgkeywords; }
		}
		
		private static string comas = ", . ! ? ; : & - ( ) | '\" / » [ ] ~ > <";
		private static string[] Comas
		{
			get
			{
				return comas.Split(" ".ToCharArray());
			}
		}
		
        private int orgTxtWordCnt;
        public int OrgTxtWordCnt
        {
            get { return orgTxtWordCnt; }            
        }
        private int cleanedTxtWordCnt;
        public int CleanedTxtWordCnt
        {
            get { return cleanedTxtWordCnt; }            
        }
        private double score;
        public double Score
        {
            get { return score; }
            set { score = value; }
        }
        private static List<string> stopwords = null;        
        private Dictionary<string, double> bagOfWords = null;        
        private Dictionary<string, double> tf = null;
        private Dictionary<string, double> tfidf = null;
        public Dictionary<string, double> Tfidf
        {
            get { return tfidf; }            
        }
        public Dictionary<string, double> TermFreqs
        {
            get { return tf; }            
        }

        public Document()
        {
            bagOfWords = new Dictionary<string, double>();
            tf = new Dictionary<string, double>();
            tfidf = new Dictionary<string, double>();
            if (stopwords == null)            
                stopwords = new List<string>(stopwordssrt.Split(",".ToCharArray()));
            if (keywords == null)
			{
				orgkeywords = TFIDF.getOrgKeywords();
                keywords = TFIDF.getKeywords();
			}
        }

        private string header;
        public string Header
        {
            get { return header; }
            set { header = value; }
        }

        private string text;
        public string Text
        {
            get 
            {
                if(text != null)
                    orgTxtWordCnt = text.Split(" ".ToCharArray()).Length;
                return text; 
            }
            set { text = value; }
        }

        public static string cleanDoc(string doc, out int cleanedTxtWordCnt, string splitter)
        {
            int ctw = 0;
            porter.Stemmer stemmer = new porter.Stemmer();
			Array.ForEach(Comas, item => doc = doc.Replace(item, ""));
            string ret = null;
            (from t in doc.ToLower().Split(splitter.ToCharArray())
             where 
			 !stopwords.Contains(t)			 
             select t).ToList().
             ForEach(ct =>
             {
                 stemmer.add(ct.ToCharArray(), ct.Length);
                 stemmer.stem();
                 ret = string.Concat(ret, stemmer.ToString(), splitter); ctw++;
             });
            
            cleanedTxtWordCnt = ctw;
            return ret;

        }

        public string CleanedText
        {
            get
            {
                return Document.cleanDoc(text, out cleanedTxtWordCnt, " ");
            }
        }

        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private void _initBagOfWords()
        {
            bagOfWords.Clear();
            List<string> docterms = new List<string>(CleanedText.Trim().Split(" ".ToCharArray()));
            foreach (string keyword in keywords.Distinct())
            {
                int cnt = (from term in docterms where term == keyword select term).Count();
                if (!bagOfWords.ContainsKey(keyword))
                    bagOfWords.Add(keyword, cnt);
            }			
        }

        private void _initTF()
        {
            tf.Clear();
            double maxbofw = bagOfWords.Values.Max();			
            foreach (string word in bagOfWords.Keys)     
			{
                tf.Add(word, bagOfWords[word] / maxbofw);				
			}
        }

        public void initTFIDF(Dictionary<string, double> idfs)
        {			
            idfs.Keys.ToList().ForEach(idfk =>
                {
                    tfidf.Add(idfk, idfs[idfk] * tf[idfk]);					
                });
        }

        public void initTF()
        {
            _initBagOfWords();
            _initTF();
        }

        public override string ToString()
        {
            return header;
        }
    }
}
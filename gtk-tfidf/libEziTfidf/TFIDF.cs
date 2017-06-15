//l.kasprzyk@gmail.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace libEziTfidf
{
    public class TFIDF
    {
        private static string dbfile, keywordsfile;
        private Dictionary<string, double> queryBagOfWords = new Dictionary<string, double>();
        private Dictionary<string, double> queryTF = new Dictionary<string, double>();
        private Dictionary<string, double> querytfidf = new Dictionary<string, double>();

        private List<Document> db;
        public List<Document> Db
        {
            get 
			{ 
				int i = 0;
				db.ForEach(d => db[i++].Score = 0);
				return db; 
			}
        }

        private Dictionary<string, double> idfs;
        private Dictionary<string, List<Document>> invertedFile;
        
        public TFIDF(string dbfile, string keywordsfile)
        {
            TFIDF.dbfile = dbfile;
            TFIDF.keywordsfile = keywordsfile;
			Document.Keywords = null;
            db = new List<Document>();
            idfs = new Dictionary<string, double>();
            invertedFile = new Dictionary<string, List<Document>>();
            initDB();
            init();
        }
		
		public static string[] getOrgKeywords()
        {            
            string stream = File.ReadAllText(TFIDF.keywordsfile);            
            return stream.Split('\n');
        }

        public static string[] getKeywords()
        {            
            string stream = File.ReadAllText(TFIDF.keywordsfile);
            int cwc = 0;
            stream = Document.cleanDoc(stream, out cwc, "\n").TrimEnd();
            return stream.Split('\n');
        }

        private void initDB()
        {
            db.Clear();
            StreamReader dbfileReader = new StreamReader(TFIDF.dbfile);
            string fdoc = null;
            int docid = 1;
            bool inheader = true;
            Document doc = new Document();			
            doc.Id = docid;
            while((fdoc = dbfileReader.ReadLine()) != null)
            {
                doc.Text = string.Concat(doc.Text, fdoc.Trim(), " ");
                if (inheader)
                {
                    doc.Header = fdoc.Trim();
                    inheader = !inheader;
                }
                if (fdoc == "" || dbfileReader.EndOfStream)
                {
                    db.Add(doc);
                    doc.initTF();
                    doc = new Document();
                    doc.Id = ++docid;
                    inheader = true;
                }
            }
        }

        private void init()
        {
            foreach (var doc in db)
            {
                foreach (string t in doc.TermFreqs.Keys)
                {
                    if (doc.TermFreqs[t] > 0)
                    {
                        if (!invertedFile.ContainsKey(t))
                            invertedFile.Add(t, new List<Document>());
                        invertedFile[t].Add(doc);
                    }
                }
            }
            List<string> leftJoin =
                (from tf in db[0].TermFreqs.Keys.ToList()
                 where !invertedFile.Keys.ToList().Contains(tf) 
                 select tf).ToList();
            leftJoin.ForEach(litm => invertedFile.Add(litm, new List<Document>()));			
            invertedFile.Keys.ToList().ForEach(k => 
                {
					if(invertedFile[k].Count > 0)
						idfs.Add(k, Math.Log10(db.Count / (double)invertedFile[k].Count));
					else
						idfs.Add(k, Math.Log10(1));					
                });
            foreach (var doc in db)
            {
                doc.initTFIDF(idfs);
            }
        }

        private double vecLength(List<double> vec)
        {
            double sum = 0;
            foreach (double d in vec)
            {
                sum += Math.Pow(d, 2);
            }
            return Math.Sqrt(sum);
        }

        public List<Document> search(string query, int maxdoc)
        {
            var res = (from r in rank(query) /*where r.Score > 0*/ orderby r.Score descending select r).
                Distinct().ToList();
            if (res.Count > maxdoc)
                return res.Take(maxdoc).ToList();
            else
                return res;
        }

        
        private void _initqueryBagOfWords(string query)
        {
            queryBagOfWords.Clear();
            int qcwc = 0;
            List<string> docterms = new List<string>(Document.cleanDoc(query, out qcwc," ").Trim().Split(" ".ToCharArray()));
            foreach (string keyword in Document.Keywords)
            {
                int cnt = (from term in docterms where term == keyword select term).ToList().Count();
                if (!queryBagOfWords.ContainsKey(keyword))
                    queryBagOfWords.Add(keyword, cnt);
            }
        }

        private void _initQueryTF()
        {
            queryTF.Clear();
            double maxbofw = queryBagOfWords.Values.Max();

            foreach (string word in queryBagOfWords.Keys)
            {
                if(maxbofw != 0)
                    queryTF.Add(word, queryBagOfWords[word] / maxbofw);
                else
                    queryTF.Add(word, 0);
            }
        }

        public void _initQueryTFIDF()
        {
            querytfidf.Clear();			
            idfs.Keys.ToList().ForEach(idfk =>
            {
                querytfidf.Add(idfk, idfs[idfk] * queryTF[idfk]);				
            });
        }

        private double similarity(List<double> v1, List<double> v2)
        {
            double sum = 0.00;
            for (int i = 0; i < v1.Count; i++)
            {
                sum += v1[i] * v2[i];
            }            
            var v1l = vecLength(v1);
            var v2l = vecLength(v2);
            if(v1l != 0 && v2l != 0)
                return (sum / (v1l * v2l));
            else
                return 0;
        }

        private List<Document> rank(string query)
        {
            foreach (var doc in db)
                doc.Score = 0;

            _initqueryBagOfWords(query);
            _initQueryTF();
            _initQueryTFIDF();            
            
            foreach (var doc in db)
            {
                doc.Score = similarity(querytfidf.Values.ToList(), doc.Tfidf.Values.ToList());
            }
            return db;
        }
    }
}


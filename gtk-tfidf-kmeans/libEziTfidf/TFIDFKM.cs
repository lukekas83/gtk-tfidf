//l.kasprzyk@gmail.com

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace libEziTfidf
{
    public class TFIDFKM
    {
		Dictionary<Document,Dictionary<Document,double>> macierzpodobienstw = null;
		private List<int> wylosowaneIdxy = new List<int>();
        private Random rand = new Random();
        private static string dbfile, keywordsfile;       
        

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
        
        public TFIDFKM(string dbfile, string keywordsfile)
        {
            TFIDFKM.dbfile = dbfile;
            TFIDFKM.keywordsfile = keywordsfile;
			Document.Keywords = null;
            db = new List<Document>();
            idfs = new Dictionary<string, double>();
            invertedFile = new Dictionary<string, List<Document>>();
            initDB();
            init();
			macierzpodobienstw = new Dictionary<Document, Dictionary<Document, double>>();
        }
		
		public static string[] getOrgKeywords()
        {            
            string stream = File.ReadAllText(TFIDFKM.keywordsfile);            
            return stream.Split('\n');
        }

        public static string[] getKeywords()
        {            
            string stream = File.ReadAllText(TFIDFKM.keywordsfile);
            int cwc = 0;
            stream = Document.cleanDoc(stream, out cwc, "\n").TrimEnd();
            return stream.Split('\n');
        }

        private void initDB()
        {
            db.Clear();
            StreamReader dbfileReader = new StreamReader(TFIDFKM.dbfile);
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
                    doc.Klasa = fdoc.Trim();
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

        private double similarity(List<double> v1, List<double> v2)
        {
            double sum = 0.00;
            for (int i = 0; i < v1.Count; i++)
            {
                sum += v1[i] * v2[i];
            }            
            double v1l = vecLength(v1);
            double v2l = vecLength(v2);
            if(v1l != 0 && v2l != 0)
                return (sum / (v1l * v2l));
            else
                return 0;
        }
		
		private void losujIdx(ref int idx)
        {
            idx = rand.Next(0, db.Count);
            if (wylosowaneIdxy.Contains(idx))
            {
                losujIdx(ref idx);
            }
            else
            {
                wylosowaneIdxy.Add(idx);
                return;
            }
        }

		private List<Document> losujKDokumentow(int k)
		{
			wylosowaneIdxy.Clear();
			List<Document> docs = new List<Document>(k);
            int idx = 0;
            for (int i = 0; i < k; i++)
            {
                losujIdx(ref idx);
                docs.Add(db[idx]);
            }
			
			return docs;
		}		
		
		private void generujmacierzpodobienstw()
		{
			macierzpodobienstw.Clear();
			foreach(Document docrow in db)
			{
				macierzpodobienstw.Add(docrow, new Dictionary<Document,double>());
				foreach(Document doccol in db)
				{
					macierzpodobienstw[docrow].Add(
						doccol, 
						similarity(docrow.Tfidf.Values.ToList(),
						doccol.Tfidf.Values.ToList()));
				}
			}
		}
		
		private List<Document> przydzielDogrupy(List<Document> centroidsDocs,ref double j)
		{	
			List<Document> union = new List<Document>();
			double _j = 0;
			var candidatedocs = 
				(from doc in db 
				 where
				 !centroidsDocs.Contains(doc)				 
				 select doc).ToList();
			
			Document docmax = null;
			double max = 0;
			
			foreach (Document doc in candidatedocs)
			{
				docmax = null;
				max = 0;
				foreach(Document sdoc in centroidsDocs)
				{
					if(macierzpodobienstw[sdoc][doc] > max)
					{
						max = macierzpodobienstw[sdoc][doc];
						docmax = sdoc;
					}
				}				
				if(max > 0)
				{
					var centroid = 
						(from cd in centroidsDocs 
				 		where cd.Id == docmax.Id 
				 		select cd).ToList()[0];
					doc.Score = max;
					doc.Parentdoc = docmax;
					centroid.Documents.Add(doc);
				}
			}
			centroidsDocs.ForEach(doc => 
			{
				union.Add(doc);
				union.AddRange((from d in doc.Documents orderby d.Score descending select d).ToList());
				doc.Documents.ForEach(d => _j += d.Score);
			});
			j = _j;
			return union;
		}

		private List<Document> przydzielDoGrupPoczatkowych(int k, ref double j)
		{	
			List<Document> seedsDoc = losujKDokumentow(k);			
			int gcnt = 1;
			foreach(Document doc in seedsDoc)
			{
				doc.Grupa = string.Concat("Grupa ", gcnt++, " ", doc.Klasa);				
				doc.Documents = new List<Document>();
			}
			return przydzielDogrupy(seedsDoc, ref j);
		}
		
		private List<Document> noweSeedy(List<Document> obecneKlastry)
		{
			List<Document> noweSeedy =  new List<Document>();			
			int gcnt = 1;
			foreach(Document doc in obecneKlastry)
			{
				if((from ns in noweSeedy where ns.Id == doc.Id select ns).Count() > 0)									
					continue;				
				
				if(doc.Documents != null && doc.Documents.Count > 0)
				{
					Document nowyCentroid = 
						(from d in doc.Documents orderby d.Score descending select d).ToList()[0];
					nowyCentroid.Score = 0;
					nowyCentroid.Parentdoc = null;
					nowyCentroid.Documents = new List<Document>();
					nowyCentroid.Grupa = string.Concat("Grupa ", gcnt++, " ", doc.Klasa);					
					noweSeedy.Add(nowyCentroid);
					
					doc.Grupa = null;
					doc.Documents = null;
				}				
			}
			
			return noweSeedy;
		}
		
		private void reset()
		{
			db.ForEach(doc =>
			{
				doc.Score = 0;
				doc.Grupa = null;
				doc.Parentdoc = null;
				doc.Documents = null;
			});
		}
		
        public List<Document> rank(int k, int iteracji, ref double j, ref int zaiterowano)
        {
			reset();
			generujmacierzpodobienstw();
			double jmax = 0;
			int nochangecnt = 1;
			List<Document> current = przydzielDoGrupPoczatkowych(k, ref j);
			List<int> bestCentroidIds = new List<int>();
			
			for(int i = 0 ; i < iteracji ; i++)
			{
				current =  przydzielDogrupy(noweSeedy(current), ref j);
				if(j > jmax)
				{
					jmax = j;
					nochangecnt--;
					bestCentroidIds.Clear();
					current.ForEach(bd => 
					{
						if(bd.Documents != null)
							bestCentroidIds.Add(bd.Id);
					});
				}
				else
				{
					nochangecnt++;
					if(nochangecnt > 2)
					{
						zaiterowano = i;
						break;
					}					
				}
			}
			reset();
			current = (from ndc in db where bestCentroidIds.Contains(ndc.Id) select ndc).ToList();
			int gcnt = 1;
			foreach(Document doc in current)
			{
				doc.Grupa = string.Concat("Grupa ", gcnt++, " ", doc.Klasa);				
				doc.Documents = new List<Document>();
			}
			current =  przydzielDogrupy(current, ref j);
			bool assert = j == jmax;
			return current;
        }
    }
}


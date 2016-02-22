using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Nodes;
using SemanticWebMovieDB.Models;

namespace SemanticWebMovieDB.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri("http://localhost:3030/ds/sparql"));
            SparqlResultSet results = endpoint.QueryWithResultSet("PREFIX movie: <http://data.linkedmdb.org/resource/movie/>"+
                "SELECT (COUNT(DISTINCT ?film) AS ?brFilm)WHERE {?film a movie:film}");
            long brFilm = 0;
            INode outValue;
            foreach (var result in results)
            {
                result.TryGetValue("brFilm", out outValue);
                brFilm = outValue.AsValuedNode().AsInteger();
            }
            long brActor = 0;
            results = endpoint.QueryWithResultSet("PREFIX movie: <http://data.linkedmdb.org/resource/movie/>"+
                "SELECT (COUNT(DISTINCT ?actor) AS ?brActor)WHERE {?actor a movie:actor}");
            foreach (var result in results)
            {
                result.TryGetValue("brActor", out outValue);
                brActor = outValue.AsValuedNode().AsInteger();
            }
            long brTriples = 0;
            results = endpoint.QueryWithResultSet("SELECT (COUNT(*) AS ?brTriples)"+ 
                "WHERE { ?s ?p ?o  }");
            foreach (var result in results)
            {
                result.TryGetValue("brTriples", out outValue);
                brTriples = outValue.AsValuedNode().AsInteger();
            }
            ViewBag.MessageMovies = "Number of movies in LinkedMDB: " + brFilm;
            ViewBag.MessageActors = "Number of actors in LinkedMDB: " + brActor;
            ViewBag.MessageTriples = "Total number of triples in LinkedMDB: " + brTriples;
            return View();
        }
        public ActionResult SearchMovie(string name)
        {
            List<Movie> movieList = new List<Movie>();
            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri("http://localhost:3030/ds/sparql"));
            SparqlResultSet results = endpoint.QueryWithResultSet("PREFIX movie: <http://data.linkedmdb.org/resource/movie/>" + 
                " PREFIX dc: <http://purl.org/dc/terms/> SELECT  ?uri ?movieName WHERE { ?uri dc:title ?movieName" +
                " FILTER(regex( str(?movieName) , \"" + name + "\", \"i\"))} ORDER BY ?movieName");
            foreach (var result in results)
            {
                Movie mov = new Movie();
                INode outValue;
                result.TryGetValue("movieName", out outValue);
                string movieName = outValue.AsValuedNode().AsString();
                result.TryGetValue("uri", out outValue);
                string uri = outValue.AsValuedNode().AsString();
                mov.title = movieName;
                mov.uri = uri;
                movieList.Add(mov);
            }
            return View(movieList);
        }
        public ActionResult SearchActor(string name)
        {
            List<Actor> actorList = new List<Actor>();
            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri("http://localhost:3030/ds/sparql"));
            SparqlResultSet results = endpoint.QueryWithResultSet("PREFIX movie: <http://data.linkedmdb.org/resource/movie/>" +
                "SELECT  ?uri ?actorName WHERE { ?uri movie:actor_name ?actorName FILTER (regex( str(?actorName) ,\"" +
                name + "\", \"i\"))} ORDER BY ?actorName");
            foreach (var result in results)
            {
                Actor act = new Actor();
                INode outValue;
                result.TryGetValue("actorName", out outValue);
                string actorName = outValue.AsValuedNode().AsString();
                act.name = actorName;
                result.TryGetValue("uri", out outValue);
                string uri = outValue.AsValuedNode().AsString();
                act.uri = uri;
                actorList.Add(act);
            }
            return View(actorList);
        }
        public ActionResult ActorDetails(string name)
        {
            Actor actor = new Actor();
            actor.name = name;
            SparqlRemoteEndpoint endpoint = new SparqlRemoteEndpoint(new Uri("http://localhost:3030/ds/sparql"));
            SparqlResultSet results = endpoint.QueryWithResultSet("PREFIX movie: <http://data.linkedmdb.org/resource/movie/>"+
                "PREFIX dbp: <http://dbpedia.org/property/> PREFIX owl: <http://www.w3.org/2002/07/owl#>"+
                "PREFIX dc: <http://purl.org/dc/terms/> PREFIX dcd: <http://purl.org/dc/elements/1.1/>"+
                "SELECT ?uri ?birthDate ?placeOfBirth ?residence ?nickName ?description ?occupation"+
                "WHERE{ ?uri movie:actor_name \""+ name +"\" ."+
                "SERVICE <http://dbpedia.org/sparql> { ?Actor dbp:name ?imeDBP ."+
                "OPTIONAL { ?Actor dbp:occupation ?occupation } OPTIONAL { ?Actor dbp:nickname ?nickName }"+
                "OPTIONAL { ?Actor dbp:birthDate ?birthDate } OPTIONAL { ?Actor dbp:placeOfBirth ?placeOfBirth }"+
                "OPTIONAL { ?Actor dbp:residence ?residence } OPTIONAL { ?Actor dcd:description ?description }"+
                "FILTER(STR(?imeDBP) = \""+ name +"\") FILTER regex(STR(?occupation), 'actor', 'i')}}");
            foreach(var result in results)
            {
                string birthDate, placeOfBirth, residence, nickName, description, occupation;
                INode outValue;
                result.TryGetValue("birthDate", out outValue);
                if (outValue == null) birthDate = "Unknown";
                else birthDate = outValue.AsValuedNode().AsString();
                actor.birthDate = birthDate;

                result.TryGetValue("placeOfBirth", out outValue);
                if (outValue == null) placeOfBirth = "Unknown";
                else placeOfBirth = outValue.AsValuedNode().AsString();
                actor.birthPlace = placeOfBirth;

                result.TryGetValue("residence", out outValue);
                if (outValue == null) residence = "Unknown";
                else residence = outValue.AsValuedNode().AsString();
                actor.residence = residence;

                result.TryGetValue("nickName", out outValue);
                if (outValue == null) nickName = "Unknown";
                else nickName = outValue.AsValuedNode().AsString();
                actor.nickname = nickName;

                result.TryGetValue("description", out outValue);
                if (outValue == null) description = "Unknown";
                else description = outValue.AsValuedNode().AsString();
                actor.description = description;

                result.TryGetValue("occupation", out outValue);
                if (outValue == null) occupation = "Unknown";
                else occupation = outValue.AsValuedNode().AsString();
                actor.occupation = occupation;
            }
            return View(actor);
        }
        public ActionResult MovieDetails(string uriString, string title, string lang)
        {
            Movie movie = new Movie();
            movie.title = title;
            Uri uri = new Uri(uriString);
            IGraph graph = new Graph();
            UriLoader.Load(graph, uri);

            foreach (Triple trip in graph.Triples)
            {
                if (trip.Predicate.AsValuedNode().ToString().Contains("runtime"))
                {
                    movie.runtime = long.Parse(trip.Object.AsValuedNode().ToString());
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("date"))
                {
                    movie.date = DateTime.Parse(trip.Object.AsValuedNode().ToString());
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("genre"))
                {
                    IGraph genre = new Graph();
                    UriLoader.Load(genre, new Uri(trip.Object.ToString()));
                    foreach (Triple genreTrip in genre.Triples)
                    {
                        if (genreTrip.Predicate.AsValuedNode().ToString().Contains("film_genre_name"))
                        {
                            movie.genre = genreTrip.Object.AsValuedNode().ToString();
                        }
                    }
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("actor"))
                {
                    IGraph actor = new Graph();
                    UriLoader.Load(actor, new Uri(trip.Object.ToString()));
                    foreach (Triple actorTrip in actor.Triples)
                    {
                        if (actorTrip.Predicate.AsValuedNode().ToString().Contains("actor_name"))
                        {
                            movie.actors.Add(actorTrip.Object.AsValuedNode().ToString());
                        }
                    }
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("director"))
                {
                    IGraph director = new Graph();
                    UriLoader.Load(director, new Uri(trip.Object.ToString()));
                    foreach (Triple dirTrip in director.Triples)
                    {
                        if (dirTrip.Predicate.AsValuedNode().ToString().Contains("director_name"))
                        {
                            movie.director = dirTrip.Object.AsValuedNode().ToString();
                        }
                    }
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("producer"))
                {
                    IGraph producer = new Graph();
                    UriLoader.Load(producer, new Uri(trip.Object.ToString()));
                    foreach (Triple prodTrip in producer.Triples)
                    {
                        if (prodTrip.Predicate.AsValuedNode().ToString().Contains("producer_name"))
                        {
                            movie.producer = prodTrip.Object.AsValuedNode().ToString();
                        }
                    }
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("writer"))
                {
                    IGraph writer = new Graph();
                    UriLoader.Load(writer, new Uri(trip.Object.ToString()));
                    foreach (Triple writerTrip in writer.Triples)
                    {
                        if (writerTrip.Predicate.AsValuedNode().ToString().Contains("writer_name"))
                        {
                            movie.writer = writerTrip.Object.AsValuedNode().ToString();
                        }
                    }
                }
                if (trip.Predicate.AsValuedNode().ToString().Contains("owl#sameAs") && trip.Object.ToString().Contains("dbpedia"))
                {
                    lang = "@" + lang;
                    IGraph dbpGraph = new Graph();
                    UriLoader.Load(dbpGraph, new Uri(Server.UrlDecode(trip.Object.ToString())));
                    foreach (Triple dbpTrip in dbpGraph.Triples)
                    {
                        if (dbpTrip.Predicate.AsValuedNode().ToString().Contains("abstract") && dbpTrip.Object.AsValuedNode().ToString().Contains(lang))
                        {
                            movie.abstractDesc = dbpTrip.Object.AsValuedNode().ToString().Split('@').First();
                        }
                    }
                }
            }

            return View(movie);
        }
    }
}

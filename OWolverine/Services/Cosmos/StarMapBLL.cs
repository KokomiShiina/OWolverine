﻿using Microsoft.Azure.Documents.Client;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OWolverine.Models.Ogame;
using Microsoft.Azure.Documents;
using System.Net;

namespace OWolverine.Services.Cosmos
{
    public class StarMapBLL
    {
        private const string DatabaseName = "StarMap";
        private const string CollectionName = "Servers";
        private static DocumentClient _client { get; set; } = new DocumentClient(new Uri(Environment.GetEnvironmentVariable("ENDPOINT_URI")), Environment.GetEnvironmentVariable("PRIMARY_KEY"));
        
        /// <summary>
        /// Create new server
        /// </summary>
        /// <param name="universe"></param>
        /// <returns></returns>
        public static async Task CreateServerDocumentIfNotExists(Universe universe)
        {
            try
            {
                await _client.ReadDocumentAsync(UriFactory.CreateDocumentUri(DatabaseName, CollectionName, universe.Id));
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionName), universe);
                }
                else
                {
                    throw;
                }
            }

        }
    }
}
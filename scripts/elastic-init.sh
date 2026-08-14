#!/bin/sh
# Elasticsearch initialization script
set -e

echo "Waiting for Elasticsearch..."

until curl -s -u "elastic:${ELASTIC_PASSWORD}" http://elasticsearch:9200 >/dev/null; do
    sleep 2
done

echo "Setting kibana_system password..."

curl -X POST \
    -u "elastic:${ELASTIC_PASSWORD}" \
    -H "Content-Type: application/json" \
    http://elasticsearch:9200/_security/user/kibana_system/_password \
    -d "{\"password\":\"${ELASTICSEARCH_PASSWORD}\"}"

echo "Elasticsearch initialization complete!"
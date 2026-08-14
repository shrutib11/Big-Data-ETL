#!/bin/sh
# NiFi Initialization & Startup Script
set -e

echo "Copying custom configurations..."
cp /opt/nifi/custom-conf/nifi.properties /opt/nifi/nifi-current/conf/nifi.properties
cp /opt/nifi/custom-conf/authorizers.xml /opt/nifi/nifi-current/conf/authorizers.xml
cp /opt/nifi/custom-conf/keystore.jks /opt/nifi/nifi-current/conf/keystore.jks
cp /opt/nifi/custom-conf/truststore.jks /opt/nifi/nifi-current/conf/truststore.jks
cp /opt/nifi/custom-conf/flow.json.gz /opt/nifi/nifi-current/conf/flow.json.gz
cp /opt/nifi/custom-conf/logback.xml /opt/nifi/nifi-current/conf/logback.xml

echo "Replacing OIDC configuration placeholders..."
sed -i "s|OIDC_DISCOVERY_URL|https://login.microsoftonline.com/${AZURE_TENANT_ID}/v2.0/.well-known/openid-configuration|g" /opt/nifi/nifi-current/conf/nifi.properties
sed -i "s|OIDC_CLIENT_ID|${AZURE_CLIENT_ID}|g" /opt/nifi/nifi-current/conf/nifi.properties
sed -i "s|OIDC_CLIENT_SECRET|${OIDC_CLIENT_SECRET}|g" /opt/nifi/nifi-current/conf/nifi.properties

echo "Starting Apache NiFi..."
exec ../scripts/start.sh

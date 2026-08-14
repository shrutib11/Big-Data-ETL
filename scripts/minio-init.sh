#!/bin/sh
# MinIO Buckets, Policies, and Users initialization script
set -e

echo "Waiting for MinIO..."
until mc alias set local-minio http://minio:9000 "${MINIO_ROOT_USER}" "${MINIO_ROOT_PASSWORD}"; do
  sleep 1
done

echo "Creating buckets..."
mc mb --ignore-existing local-minio/raw
mc mb --ignore-existing local-minio/cleansed
mc mb --ignore-existing local-minio/quarantine

echo "Importing policies..."
mc admin policy create local-minio data-developer-policy /policies/data-developer-policy.json || true
mc admin policy create local-minio operations-auditor-policy /policies/operations-auditor-policy.json || true

echo "Configuring users..."
mc admin user add local-minio "${MINIO_DEV_USER}" "${MINIO_DEV_PASSWORD}" || true
mc admin user add local-minio "${MINIO_AUDIT_USER}" "${MINIO_AUDIT_PASSWORD}" || true

echo "Attaching policies to users..."
mc admin policy attach local-minio data-developer-policy --user "${MINIO_DEV_USER}"
mc admin policy attach local-minio operations-auditor-policy --user "${MINIO_AUDIT_USER}"

echo "MinIO initialization complete!"

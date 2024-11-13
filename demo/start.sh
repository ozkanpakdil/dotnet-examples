#!/bin/bash
set -x
export OTEL_SERVICE_NAME=my-service-name
export OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
export OTEL_EXPORTER_OTLP_ENDPOINT="https://api.honeycomb.io"
export OTEL_EXPORTER_OTLP_HEADERS="x-honeycomb-team=zmcS8gznTPSMgkSFv4NRTC"
export COREHOST_TRACE=1
dotnet run
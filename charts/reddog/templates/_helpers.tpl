{{- define "reddog.commonEnv" -}}
- name: ASPNETCORE_URLS
  value: "http://+:80"
- name: DAPR_HTTP_PORT
  value: "3500"
{{- end }}

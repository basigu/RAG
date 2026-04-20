#!/bin/sh
# Generate config.json from environment variables at container startup
cat > /usr/share/nginx/html/config.json <<EOF
{
  "API_BASE_URL": "${API_BASE_URL:-}"
}
EOF

exec nginx -g 'daemon off;'

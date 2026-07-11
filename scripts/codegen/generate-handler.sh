#!/bin/bash
#
# generate-handler.sh — SpaceOS CQRS Handler Generator
# Part of SpaceOS Code Generator Toolchain (ADR-051 Phase 5.1)
#
# Usage:
#   ./generate-handler.sh <name> --type <query|command> [options]
#
# Options:
#   --type <query|command>       Handler type (required)
#   --module <name>              Module name (e.g., Procurement)
#   --repository <interface>     Repository interface (e.g., IProcurementRepository)
#   --aggregate <name>           Aggregate root name (e.g., Order, Complaint)
#   --properties <json>          Query/Command properties (JSON array)
#   --with-response              Generate Response.cs (default: true for query)
#   --with-test                  Generate unit test file (optional)
#

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SPACEOS_ROOT="/opt/spaceos"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Default values
HANDLER_NAME=""
HANDLER_TYPE=""
MODULE_NAME=""
REPOSITORY=""
AGGREGATE=""
PROPERTIES="[]"
WITH_RESPONSE=""
WITH_TEST=false

show_usage() {
    echo -e "${CYAN}SpaceOS CQRS Handler Generator (ADR-051)${NC}"
    echo ""
    echo "Usage: $0 <name> --type <query|command> [options]"
    echo ""
    echo "Arguments:"
    echo "  name                            Handler name (PascalCase, e.g., GetOrderStatus)"
    echo ""
    echo "Required Options:"
    echo "  --type <query|command>          Handler type"
    echo "  --module <name>                 Module name (e.g., Procurement)"
    echo "  --repository <interface>        Repository interface (e.g., IProcurementRepository)"
    echo "  --aggregate <name>              Aggregate root name (e.g., Order)"
    echo ""
    echo "Optional:"
    echo "  --properties <json>             Properties JSON array (default: [{"name":"Id","type":"Guid"}])"
    echo "  --with-response                 Generate Response.cs (default: true for query, false for command)"
    echo "  --with-test                     Generate unit test file"
    echo ""
    echo "Examples:"
    echo "  $0 GetOrderStatus --type query --module Procurement --repository IProcurementRepository --aggregate Order --properties '[{\"name\":\"OrderId\",\"type\":\"Guid\"}]'"
    echo "  $0 WithdrawComplaint --type command --module Procurement --repository IComplaintRepository --aggregate Complaint"
}

parse_args() {
    if [ $# -lt 1 ]; then
        show_usage
        exit 1
    fi

    if [[ "$1" == "--help" || "$1" == "-h" ]]; then
        show_usage
        exit 0
    fi

    HANDLER_NAME="$1"
    shift

    while [ $# -gt 0 ]; do
        case "$1" in
            --type)
                HANDLER_TYPE="$2"
                if [[ ! "$HANDLER_TYPE" =~ ^(query|command)$ ]]; then
                    log_error "Invalid handler type: $HANDLER_TYPE. Must be 'query' or 'command'."
                    exit 1
                fi
                shift 2
                ;;
            --module)
                MODULE_NAME="$2"
                shift 2
                ;;
            --repository)
                REPOSITORY="$2"
                shift 2
                ;;
            --aggregate)
                AGGREGATE="$2"
                shift 2
                ;;
            --properties)
                PROPERTIES="$2"
                shift 2
                ;;
            --with-response)
                WITH_RESPONSE=true
                shift
                ;;
            --with-test)
                WITH_TEST=true
                shift
                ;;
            -h|--help)
                show_usage
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_usage
                exit 1
                ;;
        esac
    done

    # Validate required parameters
    if [ -z "$HANDLER_NAME" ]; then
        log_error "Handler name is required"
        show_usage
        exit 1
    fi

    if [ -z "$HANDLER_TYPE" ]; then
        log_error "--type is required"
        show_usage
        exit 1
    fi

    if [ -z "$MODULE_NAME" ]; then
        log_error "--module is required"
        show_usage
        exit 1
    fi

    if [ -z "$REPOSITORY" ]; then
        log_error "--repository is required"
        show_usage
        exit 1
    fi

    if [ -z "$AGGREGATE" ]; then
        log_error "--aggregate is required"
        show_usage
        exit 1
    fi

    # Set default for WITH_RESPONSE
    if [ -z "$WITH_RESPONSE" ]; then
        if [ "$HANDLER_TYPE" == "query" ]; then
            WITH_RESPONSE=true
        else
            WITH_RESPONSE=false
        fi
    fi

    # Convert aggregate to lowercase for variable name
    AGGREGATE_LOWER=$(echo "${AGGREGATE:0:1}" | tr '[:upper:]' '[:lower:]')${AGGREGATE:1}
}

generate_properties_list() {
    local props_json="$1"
    local result=""

    # Simple JSON parsing for properties (assumes valid JSON)
    # Extract name and type pairs
    local count=$(echo "$props_json" | grep -o '"name"' | wc -l)

    if [ "$count" -eq 0 ]; then
        # Default property
        echo "Guid Id"
        return
    fi

    # Extract properties using jq-like bash parsing
    local i=0
    while [ $i -lt $count ]; do
        local prop_block=$(echo "$props_json" | grep -o '{[^}]*}' | sed -n "$((i+1))p")
        local name=$(echo "$prop_block" | grep -o '"name":"[^"]*"' | cut -d'"' -f4)
        local type=$(echo "$prop_block" | grep -o '"type":"[^"]*"' | cut -d'"' -f4)
        local nullable=$(echo "$prop_block" | grep -o '"nullable":[^,}]*' | cut -d':' -f2 | tr -d ' ')

        if [ -n "$name" ] && [ -n "$type" ]; then
            if [ "$nullable" == "true" ]; then
                result="$result    $type? $name"
            else
                result="$result    $type $name"
            fi

            if [ $i -lt $((count - 1)) ]; then
                result="$result,"
            fi
            result="$result\n"
        fi

        i=$((i + 1))
    done

    echo -e "$result"
}

generate_query_files() {
    local base_dir="$SPACEOS_ROOT/backend/spaceos-modules-$(echo "$MODULE_NAME" | tr '[:upper:]' '[:lower:]')/src/SpaceOS.Modules.${MODULE_NAME}.Application/Queries/${HANDLER_NAME}"

    mkdir -p "$base_dir"

    local props=$(generate_properties_list "$PROPERTIES")

    # Generate Query.cs
    cat > "$base_dir/${HANDLER_NAME}Query.cs" <<EOF
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.${MODULE_NAME}.Application.Queries.${HANDLER_NAME};

public sealed record ${HANDLER_NAME}Query(
${props}
) : IRequest<Result<${HANDLER_NAME}Response>>;
EOF

    log_success "Created: Queries/${HANDLER_NAME}/${HANDLER_NAME}Query.cs"

    # Generate QueryHandler.cs
    cat > "$base_dir/${HANDLER_NAME}QueryHandler.cs" <<EOF
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.${MODULE_NAME}.Domain.Interfaces;

namespace SpaceOS.Modules.${MODULE_NAME}.Application.Queries.${HANDLER_NAME};

public sealed class ${HANDLER_NAME}QueryHandler : IRequestHandler<${HANDLER_NAME}Query, Result<${HANDLER_NAME}Response>>
{
    private readonly ${REPOSITORY} _repository;

    public ${HANDLER_NAME}QueryHandler(${REPOSITORY} repository)
    {
        _repository = repository;
    }

    public async Task<Result<${HANDLER_NAME}Response>> Handle(${HANDLER_NAME}Query request, CancellationToken ct)
    {
        // TODO: Implement query logic
        // Example:
        // var ${AGGREGATE_LOWER} = await _repository.Get${AGGREGATE}ByIdAsync(request.${AGGREGATE}Id, ct);
        // if (${AGGREGATE_LOWER} is null)
        //     return Result<${HANDLER_NAME}Response>.NotFound();
        //
        // return Result<${HANDLER_NAME}Response>.Success(new ${HANDLER_NAME}Response(
        //     ${AGGREGATE_LOWER}.Id
        //     // ... map properties
        // ));

        throw new NotImplementedException("TODO: Implement ${HANDLER_NAME}QueryHandler");
    }
}
EOF

    log_success "Created: Queries/${HANDLER_NAME}/${HANDLER_NAME}QueryHandler.cs"

    # Generate Response.cs if requested
    if [ "$WITH_RESPONSE" == "true" ]; then
        cat > "$base_dir/${HANDLER_NAME}Response.cs" <<EOF
namespace SpaceOS.Modules.${MODULE_NAME}.Application.Queries.${HANDLER_NAME};

public sealed record ${HANDLER_NAME}Response(
    // TODO: Add response properties
    Guid Id
);
EOF

        log_success "Created: Queries/${HANDLER_NAME}/${HANDLER_NAME}Response.cs"
    fi
}

generate_command_files() {
    local base_dir="$SPACEOS_ROOT/backend/spaceos-modules-$(echo "$MODULE_NAME" | tr '[:upper:]' '[:lower:]')/src/SpaceOS.Modules.${MODULE_NAME}.Application/Commands/${HANDLER_NAME}"

    mkdir -p "$base_dir"

    local props=$(generate_properties_list "$PROPERTIES")

    # Generate Command.cs
    cat > "$base_dir/${HANDLER_NAME}Command.cs" <<EOF
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.${MODULE_NAME}.Application.Commands.${HANDLER_NAME};

public sealed record ${HANDLER_NAME}Command(
${props}
) : IRequest<Result>;
EOF

    log_success "Created: Commands/${HANDLER_NAME}/${HANDLER_NAME}Command.cs"

    # Generate CommandHandler.cs
    cat > "$base_dir/${HANDLER_NAME}CommandHandler.cs" <<EOF
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.${MODULE_NAME}.Domain.Interfaces;

namespace SpaceOS.Modules.${MODULE_NAME}.Application.Commands.${HANDLER_NAME};

public sealed class ${HANDLER_NAME}CommandHandler : IRequestHandler<${HANDLER_NAME}Command, Result>
{
    private readonly ${REPOSITORY} _repository;

    public ${HANDLER_NAME}CommandHandler(${REPOSITORY} repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(${HANDLER_NAME}Command request, CancellationToken ct)
    {
        // TODO: Implement command logic
        // Example:
        // var ${AGGREGATE_LOWER} = await _repository.Get${AGGREGATE}ByIdAsync(request.${AGGREGATE}Id, ct);
        // if (${AGGREGATE_LOWER} is null)
        //     return Result.NotFound();
        //
        // var result = ${AGGREGATE_LOWER}.DoSomething(request.Parameter);
        // if (!result.IsSuccess)
        //     return result;
        //
        // await _repository.UpdateAsync(${AGGREGATE_LOWER}, ct);
        // return await _repository.SaveChangesAsync(ct);

        throw new NotImplementedException("TODO: Implement ${HANDLER_NAME}CommandHandler");
    }
}
EOF

    log_success "Created: Commands/${HANDLER_NAME}/${HANDLER_NAME}CommandHandler.cs"
}

main() {
    parse_args "$@"

    log_info "Generating CQRS ${HANDLER_TYPE} handler: ${HANDLER_NAME}"
    log_info "Module: ${MODULE_NAME}, Aggregate: ${AGGREGATE}"

    if [ "$HANDLER_TYPE" == "query" ]; then
        generate_query_files
    else
        generate_command_files
    fi

    echo ""
    log_success "✓ Handler generation complete!"
    log_info "Next steps:"
    log_info "  1. Review generated files"
    log_info "  2. Replace TODO comments with actual implementation"
    log_info "  3. Register handler in DI container (if not auto-registered)"
}

main "$@"

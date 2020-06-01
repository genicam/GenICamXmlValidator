#pragma once

#include <Base/GCTypes.h>
#include <GenICamFwd.h>
#include <iostream>

#if (_MSC_VER>=1600)
#define STATIC_ASSERT(COND,MSG) static_assert( (COND), #MSG )
#else
#define STATIC_ASSERT(COND,MSG) typedef char static_assertion_##MSG[(COND)?1:-1]
#endif
namespace GENAPI_NAMESPACE
{
#pragma pack(push,1)
    struct Preamble
    {
        GENICAM_NAMESPACE::Version_t version;
        char magic[40];
    };
#pragma pack(pop)
    STATIC_ASSERT(sizeof (Preamble) == 46, Size_of_Preamble_unexpected);

    inline bool operator == (const Preamble& lhs, const Preamble& rhs)
    {
        bool ret = true;
        if (memcmp( lhs.magic, rhs.magic, sizeof rhs.magic ) != 0 ||
            lhs.version.Major != rhs.version.Major ||
            lhs.version.Minor != rhs.version.Minor ||
            lhs.version.SubMinor != rhs.version.SubMinor)
        {
            ret = false;
        }

        return ret;

    }
    inline bool operator !=(const Preamble& lhs, const Preamble& rhs)
    {
        const bool equal(lhs == rhs);
        return ! equal;
    }
    const Preamble& CacheFilePreamble();

    std::ostream& WritePreamble(std::ostream& os, const Preamble&p );
    std::istream& ReadPreamble(std::istream& is, Preamble &p);

    inline const Preamble& CacheFilePreamble()
    {
        static const Preamble g_CacheFilePreamble =
        {
            { GENICAM_VERSION_MAJOR, GENICAM_VERSION_MINOR, GENICAM_VERSION_SUBMINOR },
#if defined NDEBUG 
            "R"
#else
            "D"
#endif 
#if defined _MSC_VER && defined _M_IX86
            "{C248B50C-452B-430C-B8CB-E112BDF30571}"
#elif defined _MSC_VER && defined _M_AMD64
            "{2E0E4C8C-EC35-407F-982B-0990B3499701}"
#elif defined __GNUC__  && defined __linux__ 
            "{CE5EC8AE-C875-498E-8E7B-48F78F219876}"
#elif defined __APPLE__
            "{6DBF9F1F-4042-414C-B4EB-AE61EBF632D6}"
#elif defined VXWORKS
            "{F20A0F20-44AB-465C-A2F2-BC58E2E06536}"
#else
#   error Unexpected Platform
#endif
        };

        return g_CacheFilePreamble;
    }

    inline std::ostream& WritePreamble(std::ostream& os, const Preamble&p/*=CacheFilePreamble() */)
    {
        os.write(reinterpret_cast<const char*>(&p), sizeof p);

        return os;

    }

    inline std::istream& ReadPreamble(std::istream& is, Preamble &p)
    {
        is.read(reinterpret_cast<char*>(&p), sizeof p);

        if (is.gcount() != sizeof p)
        {
            is.setstate(std::ios_base::badbit);
        }
 
        return is;
    }
    inline std::istream& ReadPreambleAndCheckCurrentVersion(std::istream& is, Preamble &p)
    {
        is.read(reinterpret_cast<char*>(&p), sizeof p);

        if (p != CacheFilePreamble())
        {
            is.setstate(std::ios_base::badbit);
        }

        return is;
    }


}


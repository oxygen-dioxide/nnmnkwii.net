from   typing import List, Dict, Optional
import py_linq
import argparse
import xmltodict
import collections

def ensurelist(obj) -> list:
    if(obj is None):
        return []
    if(isinstance(obj, list)):
        return obj
    return [obj]

def setreference(reference, referenceDict:Dict[str,str]):
    if("@Include" in reference and reference["@Include"] in referenceDict):
        reference["@Version"] = referenceDict[reference["@Include"]]

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("filename", type=str)
    parser.add_argument("-f", "--framework", required=False, type=str)
    parser.add_argument("-r", "--reference", required=False, type=str, action='append')
    args = parser.parse_args()

    filename:Optional[str] = args.filename
    framework:Optional[str] = args.framework
    references:List[str] = args.reference
    referenceDict:Dict[str,str] = py_linq.Enumerable(references) \
        .select(lambda x: x.split("=")) \
        .to_dictionary(lambda x: x[0], lambda x: x[1])

    #Parse xml file use xmltodict
    with open(filename, 'r', encoding="utf-8") as f:
        doc = xmltodict.parse(f.read())
    
    #Set framework version
    if(framework is not None):
        doc["Project"]["PropertyGroup"]["TargetFramework"] = framework
    
    #Set reference version
    py_linq.Enumerable(doc["Project"]["ItemGroup"]) \
        .select_many(ensurelist) \
        .select(lambda x:x.get("PackageReference")) \
        .select_many(ensurelist) \
        .select(lambda x:setreference(x, referenceDict)) \
        .to_list()
    
    #Write back to file
    with open(filename, 'w', encoding="utf-8") as f:
        f.write(xmltodict.unparse(doc, pretty=True))


if(__name__=="__main__"):
    main()
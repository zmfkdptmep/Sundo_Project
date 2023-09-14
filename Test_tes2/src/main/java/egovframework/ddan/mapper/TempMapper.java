package egovframework.ddan.mapper;

import java.util.List;

import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.ddan.vo.LocationVo;

@Mapper
public interface TempMapper {
	
	public int insert(LocationVo loca);
	
	public List<LocationVo> getLocaList();
	
	public int deleteTemt();
}

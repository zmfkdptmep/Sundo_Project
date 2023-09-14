package egovframework.ddan.mapper;

import java.util.List;

import org.egovframe.rte.psl.dataaccess.mapper.Mapper;

import egovframework.ddan.service.MemberVo;

@Mapper
public interface MemberMapper {
	
		public List<MemberVo> getList() throws Exception;
		
}

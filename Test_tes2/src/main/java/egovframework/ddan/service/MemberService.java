package egovframework.ddan.service;

import java.util.List;

import org.springframework.stereotype.Service;

@Service
public interface MemberService {
		
	public List<MemberVo> getList() throws Exception;
	
}
